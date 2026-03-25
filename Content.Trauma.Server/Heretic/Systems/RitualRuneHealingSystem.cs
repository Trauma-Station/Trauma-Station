// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Religion;
using Content.Trauma.Shared.Heretic.Rituals;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class RitualRuneHealingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualRuneComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<HereticRitualRuneComponent, EndCollideEvent>(OnCollideEnd);
    }

    private void OnCollide(Entity<HereticRitualRuneComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<WeakToHolyComponent>(args.OtherEntity, out var weak))
            return;

        weak.IsColliding = true;
    }

    private void OnCollideEnd(Entity<HereticRitualRuneComponent> ent, ref EndCollideEvent args)
    {
        if (!TryComp<WeakToHolyComponent>(args.OtherEntity, out var weak))
            return;

        weak.IsColliding = false;
    }
}
