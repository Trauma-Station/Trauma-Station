// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Trauma.Common.Damage;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;

public abstract partial class SharedSacramentsSystem : EntitySystem
{
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private SharedStaminaSystem _stam = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<SacramentsOfPowerComponent> _sacramentsQuery = default!;

    [SubscribeLocalEvent]
    private void OnDamage(Entity<MobStateComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        AddIgnoredEntity(ent, args.Origin);
    }

    [SubscribeLocalEvent]
    private void OnStamina(Entity<MobStateComponent> ent, ref TookStaminaDamageEvent args)
    {
        if (args.Amount <= 0f)
            return;

        AddIgnoredEntity(ent, args.Source);
    }

    private void AddIgnoredEntity(EntityUid uid, EntityUid? origin)
    {
        if (origin is not { } || !_sacramentsQuery.TryComp(origin.Value, out var comp))
            return;

        if (!comp.IgnoredEntities.Add(uid))
            return;

        var heretic = Identity.Entity(origin.Value, EntityManager, uid);
        _popup.PopupEntity(Loc.GetString("heretic-sacraments-can-attack", ("heretic", heretic)), uid, uid, PopupType.Medium);
        Dirty(origin.Value, comp);
    }

    public bool ShouldBlockDamage(Entity<SacramentsOfPowerComponent?> ent, EntityUid? user)
    {
        return ent != user && _sacramentsQuery.Resolve(ent, ref ent.Comp, false) &&
            ent.Comp.State == SacramentsState.Open &&
            (user is not { } || !ent.Comp.IgnoredEntities.Contains(user.Value));
    }

    [SubscribeLocalEvent]
    private void OnBeforeStamina(Entity<SacramentsOfPowerComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value <= 0)
            return;

        if (!ShouldBlockDamage(ent.AsNullable(), args.Source))
            return;

        args.Cancelled = true;
        Pulse(ent);

        if (args.Source is not { } source)
            return;

        _stam.TakeStaminaDamage(source, args.Value);
    }

    [SubscribeLocalEvent]
    private void OnBeforeDamageChange(Entity<SacramentsOfPowerComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!ShouldBlockDamage(ent.AsNullable(), args.Origin))
            return;

        args.Cancelled = true;
        Pulse(ent);

        if (args.Origin is not { } origin)
            return;

        _dmg.ChangeDamage(origin,
            args.Damage * ent.Comp.DamageReturnRatio,
            targetPart: TargetBodyPart.Vital,
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
