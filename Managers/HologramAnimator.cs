using System;
using System.Collections;
using System.Collections.Generic;
using IForgorHologram.Configuration;
using UnityEngine;

namespace IForgorHologram.Managers
{
    public class HologramAnimator
    {
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

        public IEnumerator AnimateAndHide(List<GameObject> holograms, float duration, Action onComplete)
        {
            DespawnAnimationMode mode = PluginConfig.Instance.DespawnAnimation;

            if (mode == DespawnAnimationMode.None)
            {
                yield return new WaitForSeconds(duration);
            }
            else
            {
                float elapsed = 0f;
                Dictionary<GameObject, Vector3> initialScales = new Dictionary<GameObject, Vector3>();
                Dictionary<Renderer, Color[]> initialColors = new Dictionary<Renderer, Color[]>();

                foreach (var hol in holograms)
                {
                    if (hol != null && hol.activeSelf)
                    {
                        initialScales[hol] = hol.transform.localScale;
                        foreach (var r in hol.GetComponentsInChildren<Renderer>(true))
                        {
                            if (r != null && r.material != null)
                            {
                                var mat = r.material;
                                Color c1 = mat.HasProperty(ColorPropertyId) ? mat.GetColor(ColorPropertyId) : Color.white;
                                Color c2 = mat.HasProperty(BaseColorPropertyId) ? mat.GetColor(BaseColorPropertyId) : Color.white;
                                initialColors[r] = new[] { c1, c2 };
                            }
                        }
                    }
                }

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    foreach (var hol in holograms)
                    {
                        if (hol == null || !hol.activeSelf) continue;

                        if (mode == DespawnAnimationMode.Shrink)
                        {
                            if (initialScales.TryGetValue(hol, out Vector3 startScale))
                            {
                                hol.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                            }
                        }
                        else if (mode == DespawnAnimationMode.FadeAway)
                        {
                            foreach (var r in hol.GetComponentsInChildren<Renderer>(true))
                            {
                                if (r != null && initialColors.TryGetValue(r, out Color[] cols))
                                {
                                    var mat = r.material;
                                    if (mat != null)
                                    {
                                        if (mat.HasProperty(ColorPropertyId))
                                        {
                                            mat.SetColor(ColorPropertyId, Color.Lerp(cols[0], Color.clear, t));
                                        }
                                        if (mat.HasProperty(BaseColorPropertyId))
                                        {
                                            mat.SetColor(BaseColorPropertyId, Color.Lerp(cols[1], Color.clear, t));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    yield return null;
                }

                foreach (var kvp in initialScales)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.transform.localScale = kvp.Value;
                    }
                }
                foreach (var kvp in initialColors)
                {
                    if (kvp.Key != null && kvp.Key.material != null)
                    {
                        var mat = kvp.Key.material;
                        if (mat.HasProperty(ColorPropertyId)) mat.SetColor(ColorPropertyId, kvp.Value[0]);
                        if (mat.HasProperty(BaseColorPropertyId)) mat.SetColor(BaseColorPropertyId, kvp.Value[1]);
                    }
                }
            }

            onComplete?.Invoke();
        }
    }
}