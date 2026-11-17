#nullable enable
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using IForgorHologram.Configuration;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace IForgorHologram.UI;

[HotReload(RelativePathToLayout = @"./settings.bsml")]
[ViewDefinition("IForgorHologram.UI.settings.bsml")]
[UsedImplicitly]
public class IForgorHologramSettingsViewController : BSMLAutomaticViewController
{
    private IForgorHologramFlowCoordinator? _flowCoordinator;

    [Inject]
    public void Construct(IForgorHologramFlowCoordinator flowCoordinator)
    {
        _flowCoordinator = flowCoordinator;
    }
    
    [UIValue("Enabled")]
    public bool Enabled
    {
        get => PluginConfig.Instance.Enabled;
        set
        {
            PluginConfig.Instance.Enabled = value;
            PluginConfig.Save();
        }
    }

    [UIValue("HologramMode")]
    public bool HologramMode
    {
        get => PluginConfig.Instance.HologramMode;
        set
        {
            PluginConfig.Instance.HologramMode = value;
            PluginConfig.Save();
        }
    }

    [UIValue("AlignmentGuideOptions")]
    public List<string> AlignmentGuideOptions => new() { "None", "Real Sabers", "Guide Laser" };

    [UIValue("AlignmentGuide")]
    public string AlignmentGuide
    {
        get => PluginConfig.Instance.AlignmentGuide switch
        {
            AlignmentGuideMode.RealSabers => "Real Sabers",
            AlignmentGuideMode.GuideLaser => "Guide Laser",
            _ => "None"
        };
        set
        {
            PluginConfig.Instance.AlignmentGuide = value switch
            {
                "Real Sabers" => AlignmentGuideMode.RealSabers,
                "Guide Laser" => AlignmentGuideMode.GuideLaser,
                _ => AlignmentGuideMode.None
            };
            PluginConfig.Save();
        }
    }

    [UIValue("DespawnAnimationOptions")]
    public List<string> DespawnAnimationOptions => new() { "None", "Fade Away", "Shrink" };

    [UIValue("DespawnAnimation")]
    public string DespawnAnimation
    {
        get => PluginConfig.Instance.DespawnAnimation switch
        {
            DespawnAnimationMode.FadeAway => "Fade Away",
            DespawnAnimationMode.Shrink => "Shrink",
            _ => "None"
        };
        set
        {
            PluginConfig.Instance.DespawnAnimation = value switch
            {
                "Fade Away" => DespawnAnimationMode.FadeAway,
                "Shrink" => DespawnAnimationMode.Shrink,
                _ => DespawnAnimationMode.None
            };
            PluginConfig.Save();
        }
    }

    [UIValue("OverrideColor")]
    public bool OverrideColor
    {
        get => PluginConfig.Instance.OverrideColor;
        set
        {
            PluginConfig.Instance.OverrideColor = value;
            PluginConfig.Save();
        }
    }

    [UIValue("LeftGhostColor")]
    public Color LeftGhostColor
    {
        get => PluginConfig.Instance.LeftGhostColor;
        set
        {
            PluginConfig.Instance.LeftGhostColor = value;
            PluginConfig.Save();
        }
    }

    [UIValue("RightGhostColor")]
    public Color RightGhostColor
    {
        get => PluginConfig.Instance.RightGhostColor;
        set
        {
            PluginConfig.Instance.RightGhostColor = value;
            PluginConfig.Save();
        }
    }
    
    [UIValue("EnableAudioCue")]
    public bool EnableAudioCue
    {
        get => PluginConfig.Instance.EnableAudioCue;
        set
        {
            PluginConfig.Instance.EnableAudioCue = value;
            PluginConfig.Save();
        }
    }

    [UIValue("AudioCueVolumePercent")]
    public float AudioCueVolumePercent
    {
        get => PluginConfig.Instance.AudioCueVolumePercent;
        set
        {
            PluginConfig.Instance.AudioCueVolumePercent = value;
            PluginConfig.Save();
        }
    }

    [UIValue("EnableHaptics")]
    public bool EnableHaptics
    {
        get => PluginConfig.Instance.EnableHaptics;
        set
        {
            PluginConfig.Instance.EnableHaptics = value;
            PluginConfig.Save();
        }
    }

    [UsedImplicitly]
    public string PercentageFormatter(float value) => $"{value:0}%";
    
    [UIAction("BackButton")]
    void OnBackPressed() => _flowCoordinator?.DismissSelf();
}