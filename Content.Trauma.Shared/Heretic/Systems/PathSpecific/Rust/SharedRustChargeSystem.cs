// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Rust;

public abstract class SharedRustChargeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RustChargeComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<RustChargeComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<RustChargeComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<RustChargeComponent, StopThrowEvent>(OnStopThrow);
        SubscribeLocalEvent<RustChargeComponent, DownAttemptEvent>(OnDownAttempt);
        SubscribeLocalEvent<RustChargeComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<RustChargeComponent, KnockDownAttemptEvent>(OnKnockDownAttempt);
        SubscribeLocalEvent<RustChargeComponent, ComponentShutdown>(OnRustChargeShutdown);
        SubscribeLocalEvent<RustChargeComponent, HereticMagicCastAttemptEvent>(OnMagicAttempt);
    }

    private void OnMagicAttempt(Entity<RustChargeComponent> ent, ref HereticMagicCastAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnRustChargeShutdown(Entity<RustChargeComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || ent.Comp.HadAoeRust)
            return;

        RemCompDeferred<RustObjectsInRadiusComponent>(ent);
    }

    private void OnKnockDownAttempt(Entity<RustChargeComponent> ent, ref KnockDownAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnInteractAttempt(Entity<RustChargeComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnDownAttempt(Entity<RustChargeComponent> ent, ref DownAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnPreventCollide(Entity<RustChargeComponent> ent, ref PreventCollideEvent args)
    {
        if (!args.OtherFixture.Hard)
            return;

        var other = args.OtherEntity;

        if (!HasComp<DamageableComponent>(other) || _tag.HasTag(other, ent.Comp.IgnoreTag) ||
            ent.Comp.DamagedEntities.Contains(other))
            args.Cancelled = true;
    }

    private void OnStopThrow(Entity<RustChargeComponent> ent, ref StopThrowEvent args)
    {
        RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void OnLand(Entity<RustChargeComponent> ent, ref LandEvent args)
    {
        RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void OnCollide(Entity<RustChargeComponent> ent, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard)
            return;

        var other = args.OtherEntity;

        if (ent.Comp.DamagedEntities.Contains(other))
            return;

        _audio.PlayPredicted(ent.Comp.HitSound, ent, ent);

        ent.Comp.DamagedEntities.Add(other);

        if (!TryComp(other, out DamageableComponent? damageable) || _tag.HasTag(other, ent.Comp.IgnoreTag))
            return;

        // Damage mobs
        if (HasComp<MobStateComponent>(other))
        {
            _stun.KnockdownOrStun(other, ent.Comp.KnockdownTime);

            _damageable.TryChangeDamage((other, damageable),
                ent.Comp.Damage,
                targetPart: TargetBodyPart.Chest);

            return;
        }

        // Destroy structures
        DestroyStructure(other, ent);
    }

    protected virtual void DestroyStructure(EntityUid uid, EntityUid user)
    {
    }
}
