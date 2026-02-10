using Content.Server.AlertLevel;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Emag.Systems;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// Trauma - methods for cargo order restrictions
/// </summary>
public sealed partial class CargoSystem
{
    /// <summary>
    /// Check that the user has the account's approve access.
    /// Does nothing when emagged with an access breaker.
    /// </summary>
    public bool CheckAccessPopup(Entity<CargoOrderConsoleComponent> ent, EntityUid user, CargoAccountPrototype account)
    {
        if (!_emag.CheckFlag(ent, EmagType.Access) && !_accessReaderSystem.UserHasAccess(user, account.ApproveAccess))
        {
            ConsolePopup(user, Loc.GetString("cargo-console-order-not-allowed"));
            PlayDenySound(ent, ent.Comp);
            return false;
        }

        return true;
    }

    public bool CheckAlertPopup(Entity<CargoOrderConsoleComponent> ent, EntityUid user, CargoOrderData order, EntityUid station)
    {
        if (!_emag.CheckFlag(ent, EmagType.Interaction)
            && order.RequiredAlerts is {} alerts
            && (CompOrNull<AlertLevelComponent>(station)?.CurrentLevel is not {} current || !alerts.Contains(current)))
        {
            ConsolePopup(user, Loc.GetString("cargo-console-alert-level", ("product", order.ProductName)));
            PlayDenySound(ent, ent.Comp);
            return false;
        }

        return true;
    }
}
