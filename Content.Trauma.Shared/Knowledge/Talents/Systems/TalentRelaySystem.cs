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
        SubscribeLocalEvent<DamageTalentComponent, GetDamageModifierEvent>(OnCalculateDamage);
    }

    private void OnCalculateDodge(Entity<DodgeComponent> ent, ref GetDodgeSavingThrowEvent args)
    {
        args.Mod += 1;
    }

    private void OnCalculateDamage(Entity<DamageTalentComponent> ent, ref GetDamageModifierEvent args)
    {
        args.Mod += 1;
    }
}
