// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Religion;
using Content.Server.Damage.Systems;
using Content.Shared.Actions;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Temperature.Components;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class IceSpearSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Shared.Heretic.Components.Side.IceSpearComponent, ThrowDoHitEvent>(OnThrowDoHit,
            after: [typeof(DamageOtherOnHitSystem), typeof(SharedProjectileSystem)]);
    }

    private void OnThrowDoHit(Entity<Shared.Heretic.Components.Side.IceSpearComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        var hitNullRodUser = IsTouchSpellDenied(args.Target); // hit a null rod

        if (!HasComp<GhostComponent>(args.Target) &&
            HasComp<TemperatureComponent>(args.Target) && !hitNullRodUser)
            EnsureComp<Shared.Wizard.Traps.IceCubeComponent>(args.Target);

        if (Exists(ent.Comp.ActionId))
            _action.SetIfBiggerCooldown(ent.Comp.ActionId, ent.Comp.ShatterCooldown);

        if (TryComp(ent, out EmbeddableProjectileComponent? embeddable))
            _projectile.EmbedDetach(ent, embeddable);

        var coords = Transform(ent).Coordinates;
        _audio.PlayPvs(ent.Comp.ShatterSound, coords);
        QueueDel(ent);
    }

    private bool IsTouchSpellDenied(EntityUid target)
    {
        var ev = new BeforeCastTouchSpellEvent(target);
        RaiseLocalEvent(target, ev, true);

        return ev.Cancelled;
    }
}
