using IForgorHologram.Configuration;
using SiraUtil.Sabers;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace IForgorHologram.Managers
{
    public class HologramSpawner
    {
        private readonly SiraSaberFactory _siraSaberFactory;
        private readonly ColorManager _colorManager;

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

        [Inject]
        public HologramSpawner(
            [InjectOptional] SiraSaberFactory siraSaberFactory,
            [InjectOptional] ColorManager colorManager)
        {
            _siraSaberFactory = siraSaberFactory;
            _colorManager = colorManager;
        }

        public GameObject SpawnHologram(Saber source, SaberType saberType)
        {
            if (source == null)
                return null;

            Vector3 worldPosition = source.transform.position;
            Quaternion worldRotation = source.transform.rotation;

            GameObject hologramObj = null;

            if (_siraSaberFactory != null)
            {
                SiraSaber siraSaber = _siraSaberFactory.Spawn(saberType);
                if (siraSaber != null)
                {
                    hologramObj = siraSaber.gameObject;
                }
            }

            if (hologramObj == null)
            {
                hologramObj = Object.Instantiate(source.gameObject);
            }

            hologramObj.transform.SetPositionAndRotation(worldPosition, worldRotation);

            foreach (Collider collider in hologramObj.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    collider.enabled = false;
            }

            if (hologramObj.TryGetComponent(out Saber baseSaber) && baseSaber != null)
                baseSaber.enabled = false;

            if (hologramObj.TryGetComponent(out SiraSaber sSaber) && sSaber.Saber != null)
                sSaber.Saber.enabled = false;

            foreach (var mb in hologramObj.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null && (mb.GetType().Name.Contains("Movement") || mb.GetType().Name.Contains("Cut") || mb.GetType().Name.Contains("Trail")))
                {
                    mb.enabled = false;
                }
            }

            hologramObj.AddComponent<HologramAlignmentTracker>();
            RefreshHologramAppearance(hologramObj, saberType, source);

            hologramObj.name = $"IForgorHologram_{saberType}";
            return hologramObj;
        }

        public void RefreshHologramAppearance(GameObject hologramObj, SaberType saberType, Saber source)
        {
            if (hologramObj == null)
                return;

            if (source != null)
            {
                hologramObj.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            }

            var tracker = hologramObj.GetComponent<HologramAlignmentTracker>();
            if (tracker != null)
            {
                tracker.Setup(source, PluginConfig.Instance.HologramMode, PluginConfig.Instance.AlignmentGuide);
            }

            if (!PluginConfig.Instance.HologramMode)
            {
                ApplyStandardSaberColor(hologramObj, saberType);
            }
        }

        private Color GetSaberColor(SaberType saberType)
        {
            if (PluginConfig.Instance.OverrideColor)
            {
                return saberType == SaberType.SaberA
                    ? PluginConfig.Instance.LeftGhostColor
                    : PluginConfig.Instance.RightGhostColor;
            }
            return _colorManager != null ? _colorManager.ColorForSaberType(saberType) : Color.white;
        }

        private void ApplyStandardSaberColor(GameObject hologramObj, SaberType saberType)
        {
            Color targetColor = GetSaberColor(saberType);
            foreach (Renderer renderer in hologramObj.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is LineRenderer) continue;

                Material material = renderer.material;
                if (material != null)
                {
                    if (material.HasProperty(ColorPropertyId))
                    {
                        material.SetColor(ColorPropertyId, targetColor);
                    }
                    if (material.HasProperty(BaseColorPropertyId))
                    {
                        material.SetColor(BaseColorPropertyId, targetColor);
                    }
                    material.renderQueue = 3000;
                }
            }
        }
    }
}