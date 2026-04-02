using Content.Server.AlertLevel;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Emag.Systems;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// Trauma - methods for cargo order restrictions and destinations
/// </summary>
public sealed partial class CargoSystem
{
    private List<(string, NetEntity)> _dests = new();

    /// <summary>
    /// Check that the user has the account's approve access.
    /// Does nothing when emagged with an access breaker or for access-ignoring consoles.
    /// </summary>
    public bool CheckAccessPopup(Entity<CargoOrderConsoleComponent> ent, EntityUid user, CargoAccountPrototype account)
    {
        if (ent.Comp.IgnoreAccess || _emag.CheckFlag(ent, EmagType.Access) || _accessReaderSystem.UserHasAccess(user, account.ApproveAccess))
            return true;

        ConsolePopup(user, Loc.GetString("cargo-console-order-not-allowed"));
        PlayDenySound(ent, ent.Comp);
        return false;
    }

    public bool CheckAlertPopup(Entity<CargoOrderConsoleComponent> ent, EntityUid user, CargoProductPrototype product, EntityUid station)
    {
        if (!_emag.CheckFlag(ent, EmagType.Interaction)
            && product.RequiredAlerts is {} alerts
            && (CompOrNull<AlertLevelComponent>(station)?.CurrentLevel is not {} current || !alerts.Contains(current)))
        {
            ConsolePopup(user, Loc.GetString("cargo-console-alert-level", ("product", product.Name)));
            PlayDenySound(ent, ent.Comp);
            return false;
        }

        return true;
    }

    public List<(string, NetEntity)> GetDestinations(EntityUid console)
    {
        _dests.Clear();
        var map = Transform(console).MapID;

        var atsQuery = EntityQueryEnumerator<TradeStationComponent, TransformComponent>();
        while (atsQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != map)
                continue;

            var meta = MetaData(uid);
            _dests.Add((Name(uid, meta), GetNetEntity(uid, meta)));
        }

        var query = EntityQueryEnumerator<CargoTelepadComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != map)
                continue;

            var meta = MetaData(uid);
            _dests.Add((Name(uid, meta), GetNetEntity(uid, meta)));
        }

        return _dests;
    }
}
