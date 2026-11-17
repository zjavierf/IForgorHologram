using IForgorHologram.Managers;
using Zenject;
using JetBrains.Annotations;

namespace IForgorHologram.Installers;

[UsedImplicitly]
internal class GameInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.Bind<HologramSpawner>().AsSingle();
        Container.Bind<HologramPool>().AsSingle();
        Container.Bind<HologramAnimator>().AsSingle();
        Container.BindInterfacesAndSelfTo<PauseHologramManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
    }
}