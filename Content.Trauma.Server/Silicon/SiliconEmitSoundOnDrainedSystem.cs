// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Silicon.Death;
using Content.Shared.Sound.Components;
using Content.Server.Sound;
using Content.Shared.Mobs;

namespace Content.Trauma.Server.Silicon;

public sealed partial class EmitSoundOnCritSystem : EntitySystem
{
    [Dependency] private EmitSoundSystem _emitSound = default!;

    [SubscribeLocalEvent]
    private void OnDeath(Entity<SiliconEmitSoundOnDrainedComponent> ent, ref SiliconChargeDeathEvent args)
    {
        var spamComp = EnsureComp<SpamEmitSoundComponent>(ent);

        spamComp.MinInterval = ent.Comp.MinInterval;
        spamComp.MaxInterval = ent.Comp.MaxInterval;
        spamComp.PopUp = ent.Comp.PopUp;
        spamComp.Sound = ent.Comp.Sound;
        _emitSound.SetEnabled((ent, spamComp), true);
    }

    [SubscribeLocalEvent]
    private void OnAlive(Entity<SiliconEmitSoundOnDrainedComponent> ent, ref SiliconChargeAliveEvent args)
    {
        RemComp<SpamEmitSoundComponent>(ent); // This component is bad and I don't feel like making a janky work around because of it.
        // If you give something the SiliconEmitSoundOnDrainedComponent, know that it can't have the SpamEmitSoundComponent, and any other systems that play with it will just be broken.
    }

    [SubscribeLocalEvent]
    public void OnStateChange(Entity<SiliconEmitSoundOnDrainedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        RemComp<SpamEmitSoundComponent>(ent);
    }
}
