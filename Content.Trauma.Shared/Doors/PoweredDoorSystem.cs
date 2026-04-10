// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Power.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Prying.Components;
using Content.Shared.Prying.Systems;

namespace Content.Trauma.Shared.Doors;

public sealed class PoweredDoorSystem : EntitySystem
{
    [Dependency] private readonly PryingSystem _prying = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PoweredDoorComponent, BeforePryEvent>(OnBeforePry);
        SubscribeLocalEvent<PoweredDoorComponent, ActivateInWorldEvent>(OnActivate,
            before: [ typeof(InteractionPopupSystem) ]);
    }

    private void OnBeforePry(Entity<PoweredDoorComponent> ent, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_power.IsPowered(ent.Owner) || args.PryPowered)
            return;

        args.Message = "airlock-component-cannot-pry-is-powered-message";

        args.Cancelled = true;
    }

    private void OnActivate(Entity<PoweredDoorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex || _power.IsPowered(ent.Owner))
            return;

        _prying.TryPry(ent.Owner, args.User, out _);
    }
}
