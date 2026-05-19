// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.MobClass;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.MobClass;

[UsedImplicitly]
public sealed class MobClassSelectorBui : BoundUserInterface
{
    private MobClassSelectorWindow? _window;

    public MobClassSelectorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        base.Open();

        IoCManager.InjectDependencies(this);

        _window = this.CreateWindow<MobClassSelectorWindow>();
        _window.OpenCentered();

        _window.Specialize += Specialize;
    }

    /// <summary>
    /// Sends a predicted message in order for the entity to change their current class, to the selected one.
    /// </summary>
    private void Specialize(ProtoId<MobClassPrototype>? obj)
    {
        if (obj is not { } mobClass)
            return;

        SendPredictedMessage(new MobClassSelectedMessage(mobClass));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MobClassState selectorState)
            return;

        _window?.PopulateWindow(selectorState.Group);
    }
}
