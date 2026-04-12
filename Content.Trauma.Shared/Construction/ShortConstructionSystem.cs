// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction.Events;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Construction;

/// <summary>
/// Handles making the client open the UI.
/// Actual construction is done by <c>ConstructionSystem</c> not this.
/// </summary>
public sealed class ShortConstructionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShortConstructionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ShortConstructionComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnMapInit(Entity<ShortConstructionComponent> ent, ref MapInitEvent args)
    {
        _ui.SetUi(ent.Owner, ShortConstructionUiKey.Key, new InterfaceData("ShortConstructionBUI"));
    }

    private void OnUseInHand(Entity<ShortConstructionComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;
        var user = args.User;
        var key = ShortConstructionUiKey.Key;
        _ui.TryToggleUi(ent.Owner, key, user);
    }
}
