// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.CombatMode;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Makes the target entity melee attack itself.
/// </summary>
public sealed partial class AttackSelf : EntityEffectBase<AttackSelf>
{
    /// <summary>
    /// Try to use the held item instead of a punch attack.
    /// </summary>
    [DataField]
    public bool UseHeld = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-attack-self", ("chance", Probability), ("useHeld", UseHeld));
}

public sealed class AttackSelfEntityEvent : EntityEffectSystem<CombatModeComponent, AttackSelf>
{
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;

    private EntityQuery<MeleeWeaponComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<MeleeWeaponComponent>();
    }

    protected override void Effect(Entity<CombatModeComponent> ent, ref EntityEffectEvent<AttackSelf> args)
    {
        var user = ent.Owner;
        var weapon = user;
        if (args.Effect.UseHeld)
            weapon = _hands.GetActiveItemOrSelf(user);

        if (!_query.TryComp(weapon, out var weaponComp))
            return;

        var target = ent.Owner; // stop hitting yourself!
        var wasOn = ent.Comp.IsInCombatMode;
        _combatMode.SetInCombatMode(ent, true, ent.Comp); // need to turn on combat mode or it won't attack
        _melee.AttemptLightAttack(user, weapon, weaponComp, target);
        _combatMode.SetInCombatMode(ent, wasOn, ent.Comp); // restore it to last setting
    }
}
