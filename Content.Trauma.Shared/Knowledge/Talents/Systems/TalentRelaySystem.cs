// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Shared.Knowledge.Talents.Components;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

/// <summary>
/// This class handles all the talent relay events
/// </summary>
public sealed partial class TalentRelaySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DodgeComponent, GetDodgeSavingThrowEvent>(OnCalculateDodge);
        SubscribeLocalEvent<DodgeComponent, GetDefenseModifierEvent>(OnCalculateDefenseDodge);
        SubscribeLocalEvent<AttackTalentComponent, GetAttackModifierEvent>(OnCalculateAttack);
        SubscribeLocalEvent<DefenseTalentComponent, GetDefenseModifierEvent>(OnCalculateDefense);
        SubscribeLocalEvent<DamageTalentComponent, GetDamageModifierEvent>(OnCalculateDamage);
    }

    private void OnCalculateDodge(Entity<DodgeComponent> ent, ref GetDodgeSavingThrowEvent args)
    {
        args.Mod += 1;
    }

    private void OnCalculateDefenseDodge(Entity<DodgeComponent> ent, ref GetDefenseModifierEvent args)
    {
        args.Mod += 1;
    }

    private void OnCalculateAttack(Entity<AttackTalentComponent> ent, ref GetAttackModifierEvent args)
    {
        args.Mod += 1;
    }

    private void OnCalculateDefense(Entity<DefenseTalentComponent> ent, ref GetDefenseModifierEvent args)
    {
        args.Mod += 1;
    }

    private void OnCalculateDamage(Entity<DamageTalentComponent> ent, ref GetDamageModifierEvent args)
    {
        args.Mod += 1;
    }
}
