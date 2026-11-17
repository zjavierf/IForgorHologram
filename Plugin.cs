using IForgorHologram.Installers;
using IForgorHologram.Configuration;
using IPA;
using IPA.Loader;
using IpaLogger = IPA.Logging.Logger;
using SiraUtil.Zenject;

namespace IForgorHologram;

[Plugin(RuntimeOptions.DynamicInit)]
internal class Plugin
{
    internal static IpaLogger Log { get; private set; } = null!;
    
    [Init]
    public Plugin(IpaLogger ipaLogger, PluginMetadata pluginMetadata, Zenjector zenjector)
    {
        Log = ipaLogger;
        
        PluginConfig.Load();
        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
        
        zenjector.Install<MenuInstaller>(Location.Menu);
        
        zenjector.Install<GameInstaller>(Location.StandardPlayer);
    }
        
    [OnStart]
    public void OnApplicationStart()
    {
        Log.Debug("OnApplicationStart");
    }

    [OnExit]
    public void OnApplicationQuit()
    {
        Log.Debug("OnApplicationQuit");
    }
} 