using System;
using System.IO;
using IPA.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace IForgorHologram.Configuration;

public enum DespawnAnimationMode
{
    None = 0,
    FadeAway = 1,
    Shrink = 2
}

public enum AlignmentGuideMode
{
    None = 0,
    RealSabers = 1,
    GuideLaser = 2
}

public class PluginConfig
{
    public static PluginConfig Instance { get; set; } = new();

    [JsonProperty("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("HologramMode")]
    public bool HologramMode { get; set; } = true;

    [JsonProperty("AlignmentGuide")]
    public AlignmentGuideMode AlignmentGuide { get; set; } = AlignmentGuideMode.RealSabers;

    [JsonProperty("DespawnAnimation")]
    public DespawnAnimationMode DespawnAnimation { get; set; } = DespawnAnimationMode.FadeAway;

    [JsonProperty("OverrideColor")]
    public bool OverrideColor { get; set; } = false;

    [JsonProperty("LeftGhostColorR")]
    public float LeftGhostColorR { get; set; } = 0f;

    [JsonProperty("LeftGhostColorG")]
    public float LeftGhostColorG { get; set; } = 0.8f;

    [JsonProperty("LeftGhostColorB")]
    public float LeftGhostColorB { get; set; } = 1f;

    [JsonProperty("LeftGhostColorA")]
    public float LeftGhostColorA { get; set; } = 1f;

    [JsonIgnore]
    public Color LeftGhostColor
    {
        get => new Color(LeftGhostColorR, LeftGhostColorG, LeftGhostColorB, LeftGhostColorA);
        set
        {
            LeftGhostColorR = value.r;
            LeftGhostColorG = value.g;
            LeftGhostColorB = value.b;
            LeftGhostColorA = value.a;
        }
    }

    [JsonProperty("RightGhostColorR")]
    public float RightGhostColorR { get; set; } = 1f;

    [JsonProperty("RightGhostColorG")]
    public float RightGhostColorG { get; set; } = 0.2f;

    [JsonProperty("RightGhostColorB")]
    public float RightGhostColorB { get; set; } = 0.8f;

    [JsonProperty("RightGhostColorA")]
    public float RightGhostColorA { get; set; } = 1f;
    
    [JsonProperty("EnableAudioCue")]
    public bool EnableAudioCue { get; set; } = true;

    [JsonProperty("AudioCueVolumePercent")]
    public float AudioCueVolumePercent { get; set; } = 70f;

    [JsonProperty("EnableHaptics")]
    public bool EnableHaptics { get; set; } = true;

    [JsonIgnore]
    public Color RightGhostColor
    {
        get => new Color(RightGhostColorR, RightGhostColorG, RightGhostColorB, RightGhostColorA);
        set
        {
            RightGhostColorR = value.r;
            RightGhostColorG = value.g;
            RightGhostColorB = value.b;
            RightGhostColorA = value.a;
        }
    }

    private static readonly string ConfigPath = Path.Combine(UnityGame.UserDataPath, "IForgorHologram.json");

    public static void Load()
    {
        try
        {
            Directory.CreateDirectory(UnityGame.UserDataPath);

            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                Instance = JsonConvert.DeserializeObject<PluginConfig>(json) ?? new PluginConfig();
                Plugin.Log?.Info($"[IForgorHologram] Successfully loaded config from {ConfigPath}");
            }
            else
            {
                Save();
                Plugin.Log?.Info($"[IForgorHologram] Created default config at {ConfigPath}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.Error($"[IForgorHologram] Failed to load config: {ex.Message}\n{ex.StackTrace}");
            Instance = new PluginConfig();
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(UnityGame.UserDataPath);
            string json = JsonConvert.SerializeObject(Instance, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
            Plugin.Log?.Info($"[IForgorHologram] Successfully saved config to {ConfigPath}");
        }
        catch (Exception ex)
        {
            Plugin.Log?.Error($"[IForgorHologram] Failed to save config: {ex.Message}\n{ex.StackTrace}");
        }
    }
}