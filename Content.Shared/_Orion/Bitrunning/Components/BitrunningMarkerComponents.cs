using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Bitrunning.Components;

/// <summary>
/// Marks entities that belong to an active bitrunning domain runtime and should be cleaned up when the run ends.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningDomainRuntimeComponent : Component;

/// <summary>
/// Marker used by the domain loader to place the return point where triggering disconnecting.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningExitMarkerComponent : Component;

/// <summary>
/// Marker that indicates the primary objective area for a generated domain.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningGoalMarkerComponent : Component;

/// <summary>
/// Marker that defines preferred spawn positions for encrypted cache objectives.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningObjectiveEncryptedCacheSpawnMarkerComponent : Component
{
    [DataField]
    public EntProtoId CachePrototype = "EncryptedCacheNode";
}

/// <summary>
/// Marker that defines where delivery objective crates are spawned and which crate prototype is used.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningObjectiveCacheCrateSpawnMarkerComponent : Component
{
    [DataField]
    public EntProtoId CratePrototype = "BitrunningObjectiveCacheStructure";
}

/// <summary>
/// Marker that defines an explicit spawn position for completion reward cache crates.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningRewardCacheSpawnMarkerComponent : Component;

/// <summary>
/// Marker that defines player/avatar spawn positions if no dedicated objective marker is available.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningAvatarSpawnMarkerComponent : Component;

/// <summary>
/// Objective point that grants progress when interacted with, typically used by encrypted cache objective entities.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningObjectivePointComponent : Component
{
    [DataField]
    public int Points = 1;

    [DataField]
    public bool ConsumeOnUse = true;

    [DataField]
    public SoundSpecifier PickupSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}

/// <summary>
/// Delivery target sensor that grants objective progress when valid cargo enters it.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningObjectiveDeliveryPointComponent : Component
{
    [DataField]
    public int Points = 1;
}

/// <summary>
/// Marks an entity as valid cargo for delivery-type bitrunning objectives.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningObjectiveCargoComponent : Component;

/// <summary>
/// Marks cargo that has already been delivered to prevent duplicate scoring.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningDeliveredObjectiveCargoComponent : Component;

/// <summary>
/// Marks enemies that grant objective progress when killed in eliminate-target domains.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningDomainEnemyObjectiveComponent : Component
{
    [DataField]
    public int Points = 1;

    public EntityUid? DomainMapUid;
}

/// <summary>
/// Marks an enemy objective entity that already granted elimination progress.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningEnemyObjectiveCountedComponent : Component;

/// <summary>
/// Marks a reward cache crate that should disappear immediately after being opened.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningDespawnOnOpenComponent : Component;
