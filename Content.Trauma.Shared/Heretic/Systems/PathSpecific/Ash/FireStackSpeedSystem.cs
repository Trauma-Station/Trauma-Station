// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Components;
using Content.Shared.Movement.Systems;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

public sealed partial class FireStackSpeedSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _mod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireStackSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<FireStackSpeedComponent, FireStacksChangedEvent>(OnFireStacksChanged);
    }

    private void OnRefreshMovespeed(Entity<FireStackSpeedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp(ent, out FlammableComponent? flam) || !flam.OnFire || flam.FireStacks <= 0f)
            return;

        var mod = 1f + flam.FireStacks * ent.Comp.FireStackSpeedMultiplier;
        args.ModifySpeed(mod);
    }

    private void OnFireStacksChanged(Entity<FireStackSpeedComponent> ent, ref FireStacksChangedEvent args)
    {
        _mod.RefreshMovementSpeedModifiers(ent);
    }
}
