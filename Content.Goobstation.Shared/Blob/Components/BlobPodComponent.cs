// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Content.Trauma.Common.CollectiveMind;

namespace Content.Goobstation.Shared.Blob.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlobPodComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsZombifying;

    [DataField, AutoNetworkedField]
    public EntityUid? ZombifiedEntityUid;

    [DataField]
    public TimeSpan ZombifyDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public EntityUid? Core;

    [DataField]
    public SoundSpecifier ZombifySoundPath = new SoundPathSpecifier("/Audio/Effects/Fluids/blood1.ogg");

    [DataField]
    public SoundSpecifier ZombifyFinishSoundPath = new SoundPathSpecifier("/Audio/Effects/gib1.ogg");

    public Entity<AudioComponent>? ZombifyStingStream;
    [DataField]
    public EntityUid? ZombifyTarget;

    [DataField]
    public ProtoId<CollectiveMindPrototype> CollectiveMind = "Blobmind";
}
