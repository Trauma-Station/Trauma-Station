// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Radiation.Systems;
using Content.Shared.Mobs;
using Content.Trauma.Shared.Genetics.Abilities;

namespace Content.Trauma.Server.Genetics.Abilities;

// fucking chud shit is in server for no reason
public sealed class RadiationMutationSystem : EntitySystem
{
    [Dependency] private readonly RadiationSystem _radiation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadiationMutationComponent, MobStateChangedEvent>(OnStateChanged);
    }

    private void OnStateChanged(Entity<RadiationMutationComponent> ent, ref MobStateChangedEvent args)
    {
        _radiation.SetSourceEnabled(ent.Owner, args.NewMobState != MobState.Dead);
    }
}
