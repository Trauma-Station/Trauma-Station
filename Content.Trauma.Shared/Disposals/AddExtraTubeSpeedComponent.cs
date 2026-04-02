// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Disposals;

[RegisterComponent, NetworkedComponent]
public sealed partial class AddExtraTubeSpeedComponent : Component
{
    [DataField]
    public float Amount = 10f;

    [DataField]
    public SoundSpecifier? UpgradeSound = new SoundPathSpecifier("/Audio/Items/rped.ogg");

    [DataField]
    public float Delay = 5f;

    public EntityUid? SoundStream;
}

[Serializable, NetSerializable]
public sealed partial class AddTubeSpeedDoAfter : SimpleDoAfterEvent;
