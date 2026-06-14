using Content.Shared.Blocking;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Zombies;

public sealed partial class ZombieSystem
{
    public readonly ProtoId<NpcFactionPrototype> IIFaction = "InitialInfected";

    private bool IsUserBlocking(EntityUid uid)
        => TryComp<BlockingUserComponent>(uid, out var user) &&
            TryComp<BlockingComponent>(user.BlockingItem, out var blockComp) &&
            blockComp.IsBlocking;
}
