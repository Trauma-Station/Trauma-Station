// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BlobNodeComponent : Component
{
    [DataField]
    public TimeSpan PulseFrequency = TimeSpan.FromSeconds(4);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPulse;

    [DataField]
    public float PulseRadius = 4f;

    [DataField, AutoNetworkedField]
    public EntityUid? BlobResource;
    [DataField, AutoNetworkedField]
    public EntityUid? BlobFactory;
    /*
    [DataField, AutoNetworkedField]
    public EntityUid? BlobStorage;
    [DataField, AutoNetworkedField]
    public EntityUid? BlobTurret;
    */
}

/// <summary>
/// Event raised on tiles near a node when it pulses.
/// </summary>
[ByRefEvent]
public record struct BlobNodePulseEvent(Entity<BlobCoreComponent> Core, BlobChemPrototype Chem, bool Handled = false);

/// <summary>
/// Event raised on all special tiles of Blob Node on pulse.
/// </summary>
[ByRefEvent]
public record struct BlobSpecialPulseEvent(Entity<BlobCoreComponent> Core, BlobChemPrototype Chem);
