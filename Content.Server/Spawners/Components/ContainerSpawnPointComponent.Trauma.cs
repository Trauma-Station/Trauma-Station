using Content.Server.Spawners.EntitySystems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components;

public sealed partial class ContainerSpawnPointComponent : Component, ISpawnPoint
{
    /// <summary>
    /// Extra Jobs that can spawn at a spawnpoint
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> ExtraJobs = new();
}
