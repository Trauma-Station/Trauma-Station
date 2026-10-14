using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Trauma.Shared.Samurai;

namespace Content.Trauma.Server.Samurai;

/// <summary>
///
/// </summary>
public sealed partial class SamuraiArmorSystem : SharedSamuraiArmorSystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem Popup = default!;

    [SubscribeLocalEvent]
    private void OnRecallEnergyPolearm(Entity<SamuraiArmorComponent> ent, ref RecallEnergyPolearmEvent args)
    {
        var user = args.Performer;
        if (!Samurai.SamuraiQuery.TryComp(user, out var samurai) || samurai.EnergyPolearm == null)
            return;

        args.Handled = true;

        var energyPolearm = samurai.EnergyPolearm.Value;

        var message = _hands.TryPickupAnyHand(user, energyPolearm)
            ? "samurai-energy-polearm-recalled"
            : "samurai-recall-failed";
        Popup.PopupEntity(Loc.GetString(message), user, user);
    }
}
