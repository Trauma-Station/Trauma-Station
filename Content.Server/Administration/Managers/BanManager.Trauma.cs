using Content.Server.Database;

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager
{
    public event Action<BanDef>? OnBanCreated;
}
