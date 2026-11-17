using IForgorHologram.UI;
using JetBrains.Annotations;
using Zenject;

namespace IForgorHologram.Installers;

[UsedImplicitly]
internal class MenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<IForgorHologramMenuManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<IForgorHologramSettingsViewController>().FromNewComponentAsViewController().AsSingle();
        Container.Bind<IForgorHologramFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
    }
}