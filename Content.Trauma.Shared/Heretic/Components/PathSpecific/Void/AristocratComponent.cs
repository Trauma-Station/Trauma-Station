// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class AristocratComponent : Component
{
    [DataField]
    public float Range = 10f;

    [ViewVariables]
    public int UpdateStep = 1;

    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromMilliseconds(100);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public bool HasDied;

    [DataField]
    public float Acceleration = 2f;

    [DataField]
    public float Modifier = 1.25f;

    [DataField]
    public float Friction = 1;

    [DataField]
    public SoundSpecifier VoidsEmbrace =
        new SoundPathSpecifier("/Audio/_Goobstation/Heretic/Ambience/Antag/Heretic/VoidsEmbrace.ogg");
}
