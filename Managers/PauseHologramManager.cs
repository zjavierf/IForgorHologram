using System;
using System.Collections;
using System.Collections.Generic;
using IForgorHologram.Configuration;
using UnityEngine;
using Zenject;

namespace IForgorHologram.Managers
{
    public sealed class PauseHologramManager : MonoBehaviour, IInitializable, IDisposable
    {
        private PauseController _pauseController;
        private PauseMenuManager _pauseMenuManager;
        private SaberManager _saberManager;
        private HologramPool _hologramPool;
        private HologramAnimator _hologramAnimator;

        private readonly List<GameObject> _holograms = new List<GameObject>();
        private readonly List<Renderer> _forcedVisibleRenderers = new List<Renderer>();
        private readonly Dictionary<Collider, bool> _originalColliderStates = new Dictionary<Collider, bool>();
        private Coroutine _resumeCleanupRoutine;
        private Coroutine _showRealSabersRoutine;

        [Inject]
        private void Construct(
            [InjectOptional] PauseController pauseController,
            [InjectOptional] PauseMenuManager pauseMenuManager,
            [InjectOptional] SaberManager saberManager,
            HologramPool hologramPool,
            HologramAnimator hologramAnimator)
        {
            _pauseController = pauseController;
            _pauseMenuManager = pauseMenuManager;
            _saberManager = saberManager;
            _hologramPool = hologramPool;
            _hologramAnimator = hologramAnimator;
        }

        public void Initialize()
        {
            Plugin.Log.Info("[IForgorHologram] Initializing PauseHologramManager...");

            if (_pauseController != null)
            {
                _pauseController.didPauseEvent += OnPaused;
                _pauseController.didResumeEvent += OnResumedRequested;
            }

            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.didPressRestartButtonEvent += OnRestartPressed;
            }
        }

        public void Dispose()
        {
            if (_pauseController != null)
            {
                _pauseController.didPauseEvent -= OnPaused;
                _pauseController.didResumeEvent -= OnResumedRequested;
            }

            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.didPressRestartButtonEvent -= OnRestartPressed;
            }

            StopAllCoroutines();
            RestoreRealSaberVisibility();
            HideAllHolograms();
        }

        private void OnPaused()
        {
            if (_resumeCleanupRoutine != null)
            {
                StopCoroutine(_resumeCleanupRoutine);
                _resumeCleanupRoutine = null;
            }

            if (_showRealSabersRoutine != null)
            {
                StopCoroutine(_showRealSabersRoutine);
                _showRealSabersRoutine = null;
            }

            RestoreRealSaberVisibility();
            HideAllHolograms();

            if (!PluginConfig.Instance.Enabled)
                return;

            if (_saberManager == null)
                return;

            GameObject leftHol = _hologramPool.GetPooledHologram(SaberType.SaberA, _saberManager.leftSaber);
            if (leftHol != null) _holograms.Add(leftHol);

            GameObject rightHol = _hologramPool.GetPooledHologram(SaberType.SaberB, _saberManager.rightSaber);
            if (rightHol != null) _holograms.Add(rightHol);

            if (PluginConfig.Instance.HologramMode && PluginConfig.Instance.AlignmentGuide == AlignmentGuideMode.RealSabers)
            {
                _showRealSabersRoutine = StartCoroutine(KeepRealSabersVisibleWhilePaused());
            }
        }

        private IEnumerator KeepRealSabersVisibleWhilePaused()
        {
            yield return null;

            _forcedVisibleRenderers.Clear();
            ForceEnableSaberRenderers(_saberManager.leftSaber);
            ForceEnableSaberRenderers(_saberManager.rightSaber);
        }

        private void ForceEnableSaberRenderers(Saber saber)
        {
            if (saber == null) return;

            if (!saber.gameObject.activeSelf)
            {
                saber.gameObject.SetActive(true);
            }

            foreach (Renderer r in saber.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null && !r.enabled)
                {
                    r.enabled = true;
                    _forcedVisibleRenderers.Add(r);
                }
            }
            
            foreach (Collider col in saber.GetComponentsInChildren<Collider>(true))
            {
                if (col != null && col.enabled && !_originalColliderStates.ContainsKey(col))
                {
                    _originalColliderStates[col] = true;
                    col.enabled = false;
                }
            }
        }

        private void RestoreRealSaberVisibility()
        {
            foreach (Renderer r in _forcedVisibleRenderers)
            {
                if (r != null)
                {
                    r.enabled = false;
                }
            }
            _forcedVisibleRenderers.Clear();

            foreach (var kvp in _originalColliderStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.enabled = kvp.Value;
                }
            }
            _originalColliderStates.Clear();
        }

        private void OnResumedRequested()
        {
            if (_showRealSabersRoutine != null)
            {
                StopCoroutine(_showRealSabersRoutine);
                _showRealSabersRoutine = null;
            }

            if (_resumeCleanupRoutine != null)
                StopCoroutine(_resumeCleanupRoutine);

            _resumeCleanupRoutine = StartCoroutine(RunResumeAnimationAndHide());
        }

        private IEnumerator RunResumeAnimationAndHide()
        {
            yield return _hologramAnimator.AnimateAndHide(_holograms, 0.3f, () => {
                RestoreRealSaberVisibility();
                HideAllHolograms();
            });
            _resumeCleanupRoutine = null;
        }

        private void OnRestartPressed()
        {
            if (_showRealSabersRoutine != null)
            {
                StopCoroutine(_showRealSabersRoutine);
                _showRealSabersRoutine = null;
            }

            if (_resumeCleanupRoutine != null)
            {
                StopCoroutine(_resumeCleanupRoutine);
                _resumeCleanupRoutine = null;
            }

            RestoreRealSaberVisibility();
            HideAllHolograms();
        }

        private void HideAllHolograms()
        {
            foreach (GameObject hologram in _holograms)
            {
                if (hologram != null)
                {
                    _hologramPool.ReturnToPool(hologram);
                }
            }
            _holograms.Clear();
        }
    }
}