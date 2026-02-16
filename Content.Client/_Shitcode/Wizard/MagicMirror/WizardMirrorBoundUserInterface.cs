// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._Goobstation.Wizard.MagicMirror;
using Robust.Client.UserInterface;

namespace Content.Client._Shitcode.Wizard.MagicMirror;

public sealed class WizardMirrorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private WizardMirrorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<WizardMirrorWindow>();

        if (EntMan.TryGetComponent(Owner, out WizardMirrorComponent? mirror))
            _window.AllowedSpecies = new(mirror.AllowedSpecies);

        _window.Editor.Save += OnSave;
    }

    private void OnSave()
    {
        if (_window?.Editor?.Profile is {} profile)
            SendMessage(new WizardMirrorMessage(profile));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not WizardMirrorUiState data)
            return;

        if (_window == null)
            return;

        //_window.Editor.LoadedProfile = data.Profile.Clone();
        _window.Editor.SetProfile(data.Profile, 0);
    }
}
