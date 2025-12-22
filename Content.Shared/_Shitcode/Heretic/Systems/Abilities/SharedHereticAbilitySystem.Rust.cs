using System.Diagnostics.CodeAnalysis;
using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Explosion;
using Content.Shared.Maps;
using Content.Shared.Slippery;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitcode.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    public const string RustTile = "PlatingRust";

    public static readonly EntProtoId StatusEffectStunned = "StatusEffectStunned";

    protected virtual void SubscribeRust()
    {
        SubscribeLocalEvent<RustbringerComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<RustbringerComponent, KnockDownAttemptEvent>(OnKnockDownAttempt);
        SubscribeLocalEvent<RustbringerComponent, BeforeStatusEffectAddedEvent>(OnBeforeStatusEffect);
        SubscribeLocalEvent<RustbringerComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<RustbringerComponent, GetExplosionResistanceEvent>(OnGetExplosionResists);
        SubscribeLocalEvent<RustbringerComponent, ElectrocutionAttemptEvent>(OnElectrocuteAttempt);
        SubscribeLocalEvent<RustbringerComponent, BeforeHarmfulActionEvent>(OnBeforeHarmfulAction);
        SubscribeLocalEvent<RustbringerComponent, DamageModifyEvent>(OnModifyDamage);
    }

    private void OnModifyDamage(Entity<RustbringerComponent> ent, ref DamageModifyEvent args)
    {
        if (!IsOnRust(ent))
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.ModifierSet);
    }

    private void OnBeforeHarmfulAction(Entity<RustbringerComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Cancelled || args.Type == HarmfulActionType.Harm)
            return;

        if (!IsOnRust(ent))
            return;

        args.Cancel();
    }

    private void OnElectrocuteAttempt(Entity<RustbringerComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        if (!IsTileRust(Transform(ent).Coordinates, out _))
            return;

        args.Cancel();
    }

    private void OnGetExplosionResists(Entity<RustbringerComponent> ent, ref GetExplosionResistanceEvent args)
    {
        if (!IsOnRust(ent))
            return;

        args.DamageCoefficient *= 0f;
    }

    private void OnSlipAttempt(Entity<RustbringerComponent> ent, ref SlipAttemptEvent args)
    {
        args.NoSlip |= IsOnRust(ent);
    }

    private void OnKnockDownAttempt(EntityUid uid, RustbringerComponent comp, ref KnockDownAttemptEvent args)
    {
        args.Cancelled |= IsOnRust(uid);
    }

    private void OnBeforeStatusEffect(Entity<RustbringerComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == StatusEffectStunned)
            args.Cancelled |= IsOnRust(ent);
    }

    private void OnBeforeStaminaDamage(Entity<RustbringerComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled |= IsOnRust(ent);
    }

    public bool IsTileRust(EntityCoordinates coords, [NotNullWhen(true)] out Vector2i? tileCoords)
    {
        tileCoords = null;
        if (!_mapMan.TryFindGridAt(_transform.ToMapCoordinates(coords), out var gridUid, out var mapGrid))
            return false;

        var tileRef = _map.GetTileRef(gridUid, mapGrid, coords);
        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId];

        tileCoords = tileRef.GridIndices;
        return tileDef.ID == RustTile;
    }

    public bool IsOnRust(EntityUid uid)
        => IsTileRust(Transform(uid).Coordinates, out _);
}
