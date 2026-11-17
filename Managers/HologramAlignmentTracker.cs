using System.Collections.Generic;
using IForgorHologram.Configuration;
using UnityEngine;
using System.Linq;
using UnityEngine.XR;
using System.Reflection;
using System.IO;
using IPA.Utilities;
using UnityEngine.Networking;
using System.Collections;
using Libraries.HM.HMLib.VR;

namespace IForgorHologram.Managers
{
    public class HologramAlignmentTracker : MonoBehaviour
    {
        private Saber _targetSaber;
        private readonly List<Material> _hologramMaterials = new List<Material>();
        private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();

        private LineRenderer _guideLaser;
        private Material _laserMaterial;
        private static Texture2D _sharedLaserTexture;

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int MainTexPropertyId = Shader.PropertyToID("_MainTex");

        private static readonly Color UnalignedBlue = new Color(0.05f, 0.72f, 1.0f, 0.40f);
        private static readonly Color AlignedGreen = new Color(0.1f, 1.0f, 0.35f, 0.75f);

        private const float PositionThreshold = 0.11f;
        private const float PointAngleThreshold = 22.0f;
        private const float SaberLength = 1.0f;

        private Color _currentColor = UnalignedBlue;
        private bool _isHologramActive;
        private bool _useGuideLaser;
        private bool _wasAligned;
        private float _laserScrollOffset;
        
        // Persistent global audio source so it never gets cut off or destroyed with the saber
        private static AudioSource _globalAudioSource;
        private static AudioClip _cachedAudioClip;
        private static bool _isLoadingAudio;
        private static float _globalLastAudioPlayTime;
        private const float AudioCooldown = 0.5f;

        private HapticFeedbackManager _hapticManager;
        private HapticPresetSO _hapticPreset;

        public void Setup(Saber targetSaber, bool enableHologramMode, AlignmentGuideMode guideMode)
        {
            _targetSaber = targetSaber;
            _isHologramActive = enableHologramMode;
            _useGuideLaser = enableHologramMode && guideMode == AlignmentGuideMode.GuideLaser;

            CacheOriginalMaterials();
            EnsureGlobalAudioSource();
            CacheHapticReferences();

            if (_isHologramActive)
            {
                ApplyHologramMaterials();
                _currentColor = UnalignedBlue;
                UpdateMaterialColors(_currentColor);
            }
            else
            {
                RestoreOriginalMaterials();
            }

            SetupGuideLaser(_useGuideLaser);
        }

        private void EnsureGlobalAudioSource()
        {
            if (_globalAudioSource == null)
            {
                GameObject audioObj = new GameObject("IForgorHologram_PersistentAudioSource");
                Object.DontDestroyOnLoad(audioObj);
                _globalAudioSource = audioObj.AddComponent<AudioSource>();
                _globalAudioSource.spatialBlend = 0.0f; // Pure 2D sound
                _globalAudioSource.playOnAwake = false;
                _globalAudioSource.ignoreListenerPause = true; // Plays even when game is paused
            }

            if (_cachedAudioClip == null && !_isLoadingAudio)
            {
                StartCoroutine(LoadEmbeddedAudioClipRoutine());
            }
        }

        private IEnumerator LoadEmbeddedAudioClipRoutine()
        {
            _isLoadingAudio = true;
            Stream stream = null;
            string tempPath = string.Empty;
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                stream = assembly.GetManifestResourceStream("IForgorHologram.Resources.aligned.wav");
                if (stream == null)
                {
                    Plugin.Log?.Error("[IForgorHologram] ERROR: Could not find embedded resource 'IForgorHologram.Resources.aligned.wav'.");
                    _isLoadingAudio = false;
                    yield break;
                }

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                tempPath = Path.Combine(UnityGame.UserDataPath, "IForgorHologram_aligned_temp.wav");
                File.WriteAllBytes(tempPath, buffer);
            }
            catch (System.Exception ex)
            {
                Plugin.Log?.Error($"[IForgorHologram] Failed to write temp audio file: {ex.Message}");
                _isLoadingAudio = false;
                yield break;
            }
            finally
            {
                stream?.Dispose();
            }

            string uriPath = "file://" + tempPath;
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.WAV))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    _cachedAudioClip = DownloadHandlerAudioClip.GetContent(www);
                    if (_cachedAudioClip != null)
                    {
                        Plugin.Log?.Info($"[IForgorHologram] SUCCESS: WAV audio loaded! Length: {_cachedAudioClip.length}s");
                    }
                }
                else
                {
                    Plugin.Log?.Error($"[IForgorHologram] ERROR: UnityWebRequest failed for WAV: {www.error}");
                }
            }

            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch { }

            _isLoadingAudio = false;
        }

        private void CacheHapticReferences()
        {
            if (_hapticManager == null)
            {
                _hapticManager = Resources.FindObjectsOfTypeAll<HapticFeedbackManager>().FirstOrDefault();
            }
            if (_hapticPreset == null)
            {
                _hapticPreset = Resources.FindObjectsOfTypeAll<HapticPresetSO>()
                    .FirstOrDefault(p => p.name.Contains("Strong") || p.name.Contains("Heavy") || p.name.Contains("Rumble"))
                    ?? Resources.FindObjectsOfTypeAll<HapticPresetSO>().FirstOrDefault();
            }
        }

        private static Texture2D GetOrCreateLaserTexture()
        {
            if (_sharedLaserTexture != null)
                return _sharedLaserTexture;

            const int size = 32;
            Texture2D tex = new Texture2D(size, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size];
            for (int x = 0; x < size; x++)
            {
                float t = (float)x / size;
                float alpha = Mathf.Sin(t * Mathf.PI * 4f) > 0f ? 1.0f : 0.3f;
                pixels[x] = new Color(1f, 1f, 1f, alpha);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _sharedLaserTexture = tex;
            return _sharedLaserTexture;
        }

        private void SetupGuideLaser(bool enable)
        {
            if (enable)
            {
                if (_guideLaser == null)
                {
                    GameObject laserObj = new GameObject("IForgorGuideLaser");
                    laserObj.transform.SetParent(transform, false);

                    _guideLaser = laserObj.AddComponent<LineRenderer>();
                    _guideLaser.positionCount = 2;
                    _guideLaser.startWidth = 0.016f;
                    _guideLaser.endWidth = 0.003f;
                    _guideLaser.useWorldSpace = true;

                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader == null) shader = Shader.Find("Unlit/Transparent");

                    _laserMaterial = new Material(shader);
                    if (_laserMaterial.HasProperty(MainTexPropertyId))
                    {
                        _laserMaterial.SetTexture(MainTexPropertyId, GetOrCreateLaserTexture());
                        _laserMaterial.mainTextureScale = new Vector2(8f, 1f);
                    }
                    _laserMaterial.renderQueue = 3150;
                    _guideLaser.material = _laserMaterial;
                }
                _guideLaser.enabled = true;
            }
            else if (_guideLaser != null)
            {
                _guideLaser.enabled = false;
            }
        }

        private void CacheOriginalMaterials()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r != null && !(r is LineRenderer) && !_originalMaterials.ContainsKey(r))
                {
                    _originalMaterials[r] = r.sharedMaterials;
                }
            }
        }

        private void ApplyHologramMaterials()
        {
            foreach (Material oldMat in _hologramMaterials)
            {
                if (oldMat != null) Destroy(oldMat);
            }
            _hologramMaterials.Clear();

            Shader holoShader = Shader.Find("Sprites/Default");
            if (holoShader == null) holoShader = Shader.Find("Unlit/Transparent");

            foreach (var kvp in _originalMaterials)
            {
                Renderer r = kvp.Key;
                if (r == null) continue;
                
                Material[] newMats = new Material[kvp.Value.Length];
                for (int i = 0; i < newMats.Length; i++)
                {
                    Material mat = new Material(holoShader);
                    mat.renderQueue = 3100;
                    newMats[i] = mat;
                    _hologramMaterials.Add(mat);
                }
                r.materials = newMats;
            }
        }

        private void RestoreOriginalMaterials()
        {
            foreach (var kvp in _originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.materials = kvp.Value;
                }
            }

            foreach (Material oldMat in _hologramMaterials)
            {
                if (oldMat != null) Destroy(oldMat);
            }
            _hologramMaterials.Clear();
        }

        private void OnDisable()
        {
            if (_guideLaser != null)
            {
                _guideLaser.enabled = false;
            }
        }

        private void Update()
        {
            if (!_isHologramActive || _targetSaber == null)
                return;

            Vector3 livePos = _targetSaber.transform.position;
            Quaternion liveRot = _targetSaber.transform.rotation;

            float posDiff = Vector3.Distance(livePos, transform.position);
            float pointAngleDiff = Vector3.Angle(liveRot * Vector3.forward, transform.rotation * Vector3.forward);

            bool isAligned = posDiff <= PositionThreshold && pointAngleDiff <= PointAngleThreshold;
            
            if (isAligned && !_wasAligned)
            {
                TriggerAlignmentFeedback();
            }
            _wasAligned = isAligned;

            Color targetColor = isAligned ? AlignedGreen : UnalignedBlue;
            
            float pulse = Mathf.Sin(Time.unscaledTime * 5.5f) * 0.35f;
            targetColor.a = Mathf.Clamp01(targetColor.a + pulse);

            _currentColor = Color.Lerp(_currentColor, targetColor, Time.unscaledDeltaTime * 14f);

            UpdateMaterialColors(_currentColor);

            if (_useGuideLaser && _guideLaser != null && _guideLaser.enabled)
            {
                Vector3 liveTip = livePos + (liveRot * Vector3.forward * SaberLength);
                _guideLaser.SetPosition(0, livePos);
                _guideLaser.SetPosition(1, liveTip);

                _laserScrollOffset = (_laserScrollOffset - Time.unscaledDeltaTime * 3.5f) % 1f;
                if (_laserMaterial != null && _laserMaterial.HasProperty(MainTexPropertyId))
                {
                    _laserMaterial.mainTextureOffset = new Vector2(_laserScrollOffset, 0f);
                }

                Color laserColor = isAligned
                    ? new Color(0.1f, 1.0f, 0.45f, 0.9f)
                    : new Color(0.0f, 0.75f, 1.0f, 0.75f);

                _guideLaser.startColor = laserColor;
                _guideLaser.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.1f);
            }
        }
        
        private void TriggerAlignmentFeedback()
        {
            if (_targetSaber == null) return;

            if (PluginConfig.Instance.EnableAudioCue)
            {
                if (Time.unscaledTime - _globalLastAudioPlayTime >= AudioCooldown)
                {
                    if (_globalAudioSource != null && _cachedAudioClip != null)
                    {
                        float finalVolume = Mathf.Clamp01(PluginConfig.Instance.AudioCueVolumePercent / 100f);
                        _globalAudioSource.PlayOneShot(_cachedAudioClip, finalVolume);
                        _globalLastAudioPlayTime = Time.unscaledTime;
                        Plugin.Log?.Info($"[IForgorHologram] PERSISTENT AUDIO PLAYED. Volume: {PluginConfig.Instance.AudioCueVolumePercent}%");
                    }
                    else
                    {
                        Plugin.Log?.Warn("[IForgorHologram] Audio skipped: _globalAudioSource or _cachedAudioClip is null.");
                    }
                }
            }

            if (PluginConfig.Instance.EnableHaptics)
            {
                try
                {
                    if (_hapticManager != null && _hapticPreset != null)
                    {
                        XRNode node = _targetSaber.saberType == SaberType.SaberA ? XRNode.LeftHand : XRNode.RightHand;
                        _hapticManager.PlayHapticFeedback(node, _hapticPreset);
                    }
                }
                catch (System.Exception ex)
                {
                    Plugin.Log?.Error($"[IForgorHologram] Failed to play haptics: {ex.Message}");
                }
            }
        }

        private void UpdateMaterialColors(Color col)
        {
            foreach (Material mat in _hologramMaterials)
            {
                if (mat == null) continue;

                if (mat.HasProperty(ColorPropertyId))
                    mat.SetColor(ColorPropertyId, col);

                if (mat.HasProperty(BaseColorPropertyId))
                    mat.SetColor(BaseColorPropertyId, col);
            }
        }

        private void OnDestroy()
        {
            foreach (Material mat in _hologramMaterials)
            {
                if (mat != null) Destroy(mat);
            }
            _hologramMaterials.Clear();

            if (_laserMaterial != null)
            {
                Destroy(_laserMaterial);
            }
        }
    }
}