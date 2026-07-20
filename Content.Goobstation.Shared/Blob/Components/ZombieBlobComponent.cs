// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ZombieBlobComponent : Component
{
    [DataField]
    public List<string> OldFactions = new();

    [DataField, AutoNetworkedField]
    public EntityUid BlobPodUid;

    public float? OldColdDamageThreshold;

    [DataField]
    public Dictionary<string, int> DisabledFixtureMasks = new();

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");

    [DataField, AutoNetworkedField]
    public bool CanShoot = false;

    [DataField]
    public ProtoId<CollectiveMindPrototype> CollectiveMindAdded = "Blobmind";
}
