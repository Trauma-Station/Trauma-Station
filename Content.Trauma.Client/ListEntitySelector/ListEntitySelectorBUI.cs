// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.ListEntitySelector;
using JetBrains.Annotations;

namespace Content.Trauma.Client.ListEntitySelector;

[UsedImplicitly]
public sealed partial class ListEntitySelectorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ListEntitySelectorWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ListEntitySelectorWindow>();
        _window.OpenCentered();

        _window.OnPressEntity += OnPressEntity;
    }

    private void OnPressEntity(EntityUid entity)
    {
        SendPredictedMessage(new ListEntitySelectorMessage(EntMan.GetNetEntity(entity)));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ListEntitySelectorState selectorState)
            return;

        _window?.PopulateWindow(selectorState.Entities, selectorState.Title);
    }
}
