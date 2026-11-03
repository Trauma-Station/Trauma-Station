// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Weapons;

namespace Content.Trauma.Shared.BloodCult.Constructs.ArtificerHeal;

/// <summary>
/// Heals constructs when they are attacked by artificer.
/// </summary>
public sealed partial class ArtificerHealSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnBeforeHarmfulAction(Entity<ConstructComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (args.Cancelled || args.Type != HarmfulActionType.Harm)
            return;

        if (TryComp<ArtificerHealComponent>(args.User, out var artificer))
        {
            if (_damageable.TryChangeDamage(ent.Owner, artificer.Damage, true, false))
                _popup.PopupEntity("You partially restore this construct to its intended form.", ent.Owner, args.User);

            args.Cancelled = true;
        }
    }
}
