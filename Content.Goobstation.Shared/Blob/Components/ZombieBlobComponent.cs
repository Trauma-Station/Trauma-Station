// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Robust.Shared.Audio;
using Content.Trauma.Common.CollectiveMind;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ZombieBlobComponent : Component
{
    public List<string> OldFactions = new();

    [AutoNetworkedField]
    public EntityUid BlobPodUid = default!;

    public float? OldColdDamageThreshold = null;

    [ViewVariables]
    public Dictionary<string, int> DisabledFixtureMasks { get; } = new();

    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");

    [DataField, AutoNetworkedField]
    public bool CanShoot = false;

    [DataField]
    public ProtoId<CollectiveMindPrototype> CollectiveMindAdded = "Blobmind";
}
