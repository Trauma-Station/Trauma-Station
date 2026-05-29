using Content.Shared.Administration;
using Robust.Shared.Player;

namespace Content.Server.Administration.Notes;

public sealed partial class AdminNotesManager
{
    public bool CanWatchlist(ICommonSession admin)
    {
        return _admins.HasAdminFlag(admin, AdminFlags.Watchlist);
    }
}
