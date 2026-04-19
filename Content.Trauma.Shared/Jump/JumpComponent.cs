// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Jump;

[RegisterComponent]
public sealed partial class JumpComponent : Component
{
    [DataField]
    public SoundSpecifier? JumpSound;

    [DataField]
    public float JumpSpeed = 7f;

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(4);

    [DataField]
    public EntProtoId JumpAction = "ActionJumpXenomorph";

    [ViewVariables]
    public EntityUid? JumpActionEntity;
}

public sealed partial class JumpActionEvent : WorldTargetActionEvent;
