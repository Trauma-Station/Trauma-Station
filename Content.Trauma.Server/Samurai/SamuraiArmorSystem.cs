

using Content.Shared.Hands.EntitySystems;
using Content.Trauma.Shared.Samurai;


namespace Content.Trauma.Server.Samurai;

/// <summary>
///
/// </summary>
public abstract partial class SamuraiArmorSystem : SharedSamuraiArmorSystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedSamuraiSystem _samurai = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnRecallEnergyPolearm(Entity<SamuraiArmorComponent> ent, ref RecallEnergyPolearmEvent args)
    {
        var (uid, comp) = ent;
        var user = args.Performer;
        if (!_samurai.SamuraiQuery.TryComp(user, out var samurai) || samurai.EnergyPolearm == null)
            return;

        args.Handled = true;

        var energyPolearm = samurai.EnergyPolearm.Value;

        // TODO: teleporting into belt slot
        var message = _hands.TryPickupAnyHand(user, energyPolearm)
            ? "ninja-katana-recalled"
            : "ninja-hands-full";
        Popup.PopupEntity(Loc.GetString(message), user, user);
    }
}
