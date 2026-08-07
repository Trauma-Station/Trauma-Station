// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.BlockTeleport;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Explosion;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.StatusEffectNew;
using Content.Shared.Temperature;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;

public abstract partial class SharedBladeArenaSystem : EntitySystem
{
    public static readonly EntProtoId StatusEffectStunned = "StatusEffectStunned";

    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<InsideArenaComponent> _insideQuery = default!;
    [Dependency] protected EntityQuery<HereticArenaParticipantComponent> ParticipantQuery = default!;

    [SubscribeLocalEvent]
    private void OnElectrocuteAttempt(Entity<HereticArenaParticipantComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        if (IsInsideArena(ent))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBeforeStatusEffect(Entity<HereticArenaParticipantComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == StatusEffectStunned)
            args.Cancelled |= IsInsideArena(ent);
    }

    [SubscribeLocalEvent]
    private void OnBeforeStaminaDamage(Entity<HereticArenaParticipantComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled |= IsInsideArena(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetExplosionResists(Entity<HereticArenaParticipantComponent> ent, ref GetExplosionResistanceEvent args)
    {
        if (!IsInsideArena(ent))
            return;

        args.DamageCoefficient = 0f;
    }

    [SubscribeLocalEvent]
    private void OnDamageModify(Entity<HereticArenaParticipantComponent> ent, ref DamageModifyEvent args)
    {
        if (!IsInsideArena(ent))
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.ModifierSet);
    }

    [SubscribeLocalEvent]
    private void OnSlipAttempt(Entity<HereticArenaParticipantComponent> ent, ref SlipAttemptEvent args)
    {
        args.NoSlip |= IsInsideArena(ent);
    }

    [SubscribeLocalEvent]
    private void OnBeforeHeatExchange(Entity<HereticArenaParticipantComponent> ent, ref BeforeHeatExchangeEvent args)
    {
        args.Cancelled |= IsInsideArena(ent);
    }

    [SubscribeLocalEvent]
    private void OnPreventCollide(Entity<HereticArenaOuterWallComponent> ent, ref PreventCollideEvent args)
    {
        var other = args.OtherEntity;
        args.Cancelled = ParticipantQuery.TryComp(other, out var participant) && participant.IsVictor ||
                         HasComp<GhoulComponent>(other);
    }

    [SubscribeLocalEvent]
    private void OnTeleportAttempt(Entity<HereticArenaParticipantComponent> ent, ref TeleportAttemptEvent args)
    {
        if (ent.Comp.IsVictor)
            return;

        args.Cancelled = true;

        if (args.Message == null)
            return;

        var msg = Loc.GetString(args.Message);
        _popup.PopupEntity(msg, ent, ent);
    }

    protected bool IsInsideArena(EntityUid uid)
        => _insideQuery.HasComp(uid);
}
