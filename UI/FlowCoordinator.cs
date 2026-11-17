using BeatSaberMarkupLanguage;
using HMUI;
using Zenject;

namespace IForgorHologram.UI;

public class IForgorHologramFlowCoordinator : FlowCoordinator
{
    private IForgorHologramSettingsViewController _settingsViewController = null!;

    [Inject]
    public void Construct(IForgorHologramSettingsViewController settingsViewController)
    {
        _settingsViewController = settingsViewController;
    }

    protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
    {
        if (firstActivation)
        {
            showBackButton = false;
            ProvideInitialViewControllers(_settingsViewController);
        }
    }

    public void DismissSelf()
    {
        BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
    }

    protected override void BackButtonWasPressed(ViewController topViewController)
    {
        BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this);
    }
}