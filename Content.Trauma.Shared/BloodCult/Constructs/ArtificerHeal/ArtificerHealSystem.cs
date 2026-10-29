// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.BloodCult.Constructs.ArtificerHeal;

/// <summary>
/// Tracks constructs in the blood cult gamerule.
/// </summary>
public sealed partial class ArtificerHealSystem : EntitySystem
{
    [Dependency] protected DamageableSystem _damageable = default!;

    [SubscribeLocalEvent]
    private void OnBeforeHarmfulAction(Entity<ConstructComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Args.Cancelled || args.Args.Type != HarmfulActionType.Harm)
            return;

        if (TryComp<ArtificerHealComponent>(args.User, out var artificer))
        {
            _damageable.TryChangeDamage(target.Value, artificer.Damage, true, false);

            rgs.Args.ShouldParry = false;
            args.Args.Cancelled = true;
        }
    }

}
