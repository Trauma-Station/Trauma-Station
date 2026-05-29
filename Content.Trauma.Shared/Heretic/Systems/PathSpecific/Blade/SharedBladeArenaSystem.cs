// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.BlockTeleport;
using Content.Goobstation.Common.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Explosion;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.StatusEffectNew;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticArenaParticipantComponent, TeleportAttemptEvent>(OnTeleportAttempt);
        SubscribeLocalEvent<HereticArenaParticipantComponent, TemperatureImmunityEvent>(OnTempImmunity);
        SubscribeLocalEvent<HereticArenaParticipantComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<HereticArenaParticipantComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<HereticArenaParticipantComponent, GetExplosionResistanceEvent>(OnGetExplosionResists);
        SubscribeLocalEvent<HereticArenaParticipantComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<HereticArenaParticipantComponent, BeforeStatusEffectAddedEvent>(OnBeforeStatusEffect);
        SubscribeLocalEvent<HereticArenaParticipantComponent, ElectrocutionAttemptEvent>(OnElectrocuteAttempt);

        SubscribeLocalEvent<HereticArenaOuterWallComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnElectrocuteAttempt(Entity<HereticArenaParticipantComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        if (IsInsideArena(ent))
            args.Cancel();
    }

    private void OnBeforeStatusEffect(Entity<HereticArenaParticipantComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == StatusEffectStunned)
            args.Cancelled |= IsInsideArena(ent);
    }

    private void OnBeforeStaminaDamage(Entity<HereticArenaParticipantComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled |= IsInsideArena(ent);
    }

    private void OnGetExplosionResists(Entity<HereticArenaParticipantComponent> ent, ref GetExplosionResistanceEvent args)
    {
        if (!IsInsideArena(ent))
            return;

        args.DamageCoefficient = 0f;
    }

    private void OnDamageModify(Entity<HereticArenaParticipantComponent> ent, ref DamageModifyEvent args)
    {
        if (!IsInsideArena(ent))
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.ModifierSet);
    }

    private void OnSlipAttempt(Entity<HereticArenaParticipantComponent> ent, ref SlipAttemptEvent args)
    {
        args.NoSlip |= IsInsideArena(ent);
    }

    private void OnTempImmunity(Entity<HereticArenaParticipantComponent> ent, ref TemperatureImmunityEvent args)
    {
        if (!IsInsideArena(ent))
            return;

        args.CurrentTemperature = Atmospherics.T37C;
    }

    private void OnPreventCollide(Entity<HereticArenaOuterWallComponent> ent, ref PreventCollideEvent args)
    {
        var other = args.OtherEntity;
        args.Cancelled = ParticipantQuery.TryComp(other, out var participant) && participant.IsVictor ||
                         HasComp<GhoulComponent>(other);
    }

    private void OnTeleportAttempt(Entity<HereticArenaParticipantComponent> ent, ref TeleportAttemptEvent args)
    {
        if (ent.Comp.IsVictor)
            return;

        args.Cancelled = true;

        if (args.Message == null)
            return;

        var msg = Loc.GetString(args.Message);
        if (args.Predicted)
            _popup.PopupClient(msg, ent, ent);
        else
            _popup.PopupEntity(msg, ent, ent);
    }

    protected bool IsInsideArena(EntityUid uid)
    {
        return _insideQuery.HasComp(uid);
    }
}
