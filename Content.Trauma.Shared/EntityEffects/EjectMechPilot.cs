// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Stunnable;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class EjectMechPilot : EntityEffectBase<EjectMechPilot>
{
    [DataField]
    public TimeSpan ParalyzeDuration = TimeSpan.Zero;
}

public sealed partial class EjectMechPilotEffectSystem : EntityEffectSystem<MechComponent, EjectMechPilot>
{
    [Dependency] private SharedMechSystem _mech = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void Effect(Entity<MechComponent> ent, ref EntityEffectEvent<EjectMechPilot> args)
    {
        if (ent.Comp.PilotSlot.ContainedEntity is not { } pilot)
            return;

        _mech.TryEject(ent, ent.Comp, pilot);
        if (args.Effect.ParalyzeDuration > TimeSpan.Zero)
            _stun.TryUpdateParalyzeDuration(pilot, args.Effect.ParalyzeDuration);
    }
}
