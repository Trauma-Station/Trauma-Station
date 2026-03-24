// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;

[RegisterComponent, NetworkedComponent]
public sealed partial class AristocratComponent : Component
{
    [DataField]
    public float UpdateDelay = 0.1f;

    [DataField]
    public float Range = 10f;

    [ViewVariables]
    public int UpdateStep = 1;

    [ViewVariables]
    public float UpdateTimer = 0f;

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
