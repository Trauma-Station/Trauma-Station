// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Polymorph.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Polymorph;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed partial class AshJauntSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AshJauntComponent, PolymorphedEvent>(OnPolymorph);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<AshJauntComponent, MovementSpeedModifierComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var jaunt, out var movement, out var xform))
        {
            if (jaunt.SpawnedOutEffect || now < jaunt.EndTime)
                continue;

            jaunt.SpawnedOutEffect = true;

            Spawn(jaunt.OutEffect, xform.Coordinates);

            _movement.ChangeBaseSpeed(uid, 0f, 0f, 0f, movement);
        }
    }

    private void OnPolymorph(Entity<AshJauntComponent> ent, ref PolymorphedEvent args)
    {
        if (args.IsRevert || args.NewEntity != ent.Owner ||
            !TryComp(ent, out PolymorphedEntityComponent? polymorphed) ||
            polymorphed.Configuration.Duration is not { } duration)
            return;

        ent.Comp.EndTime = _timing.CurTime + TimeSpan.FromSeconds(duration) - ent.Comp.EffectDuration;
    }
}
