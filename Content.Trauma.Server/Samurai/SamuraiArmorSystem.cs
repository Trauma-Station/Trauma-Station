// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Trauma.Shared.Samurai;

namespace Content.Trauma.Server.Samurai;

/// <summary>
///
/// </summary>
public sealed partial class SamuraiArmorSystem : SharedSamuraiArmorSystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedProjectileSystem _projectile = default!;

    [SubscribeLocalEvent]
    private void OnRecallEnergyPolearm(Entity<SamuraiArmorComponent> ent, ref RecallEnergyPolearmEvent args)
    {
        var user = args.Performer;
        if (!Samurai.SamuraiQuery.TryComp(user, out var samurai) || samurai.EnergyPolearm == null)
            return;

        args.Handled = true;

        var energyPolearm = samurai.EnergyPolearm.Value;

        if (TryComp(energyPolearm, out EmbeddableProjectileComponent? embeddable) && embeddable.EmbeddedIntoUid != null)
            _projectile.EmbedDetach(energyPolearm, embeddable);

        var message = _hands.TryPickupAnyHand(user, energyPolearm)
            ? "samurai-energy-polearm-recalled"
            : "samurai-recall-failed";
        _popup.PopupEntity(Loc.GetString(message), user, user);
    }
}
