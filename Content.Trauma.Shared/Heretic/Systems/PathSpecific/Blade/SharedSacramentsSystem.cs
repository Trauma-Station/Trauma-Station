// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;

public abstract partial class SharedSacramentsSystem : EntitySystem
{
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private SharedStaminaSystem _stam = default!;

    [SubscribeLocalEvent]
    private void OnBeforeThrow(Entity<SacramentsOfPowerComponent> ent, ref BeforeThrowEvent args)
    {
        if (args.Cancelled || ent.Comp.State != SacramentsState.Open)
            return;

        var thrownItem = args.ItemUid;
        var ev = new AttemptPacifiedThrowEvent(thrownItem, ent);
        RaiseLocalEvent(thrownItem, ref ev);

        if (ev.Cancelled)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnAttackAttempt(Entity<SacramentsOfPowerComponent> ent, ref AttackAttemptEvent args)
    {
        if (ent.Comp.State == SacramentsState.Open)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnShotAttempted(Entity<SacramentsOfPowerComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.State == SacramentsState.Open)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBeforeStamina(Entity<SacramentsOfPowerComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (ent.Comp.State != SacramentsState.Open || args.Value <= 0f || args.Source == ent.Owner)
            return;

        args.Cancelled = true;
        Pulse(ent);

        if (args.Source is not { } source || HasComp<SacramentsOfPowerComponent>(source))
            return;

        _stam.TakeStaminaDamage(source, args.Value, source: ent);
    }

    [SubscribeLocalEvent]
    private void OnBeforeDamageChange(Entity<SacramentsOfPowerComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (ent.Comp.State != SacramentsState.Open || !args.Damage.AnyPositive() || args.Origin == ent.Owner)
            return;

        args.Cancelled = true;
        Pulse(ent);

        if (args.Origin is not { } origin || HasComp<SacramentsOfPowerComponent>(origin))
            return;

        _dmg.ChangeDamage(origin,
            args.Damage * ent.Comp.DamageReturnRatio,
            targetPart: TargetBodyPart.Vital,
            origin: ent,
            canMiss: false);
    }

    protected virtual void Pulse(EntityUid ent) { }
}

[Serializable, NetSerializable]
public sealed class SacramentsPulseEvent(NetEntity entity) : EntityEventArgs
{
    public NetEntity Entity = entity;
}

[Serializable, NetSerializable]
public enum SacramentsKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum SacramentsState : byte
{
    Opening,
    Open,
    Closing
}
