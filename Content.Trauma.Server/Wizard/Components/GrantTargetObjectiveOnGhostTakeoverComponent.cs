// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;

namespace Content.Trauma.Server.Wizard.Components;

[RegisterComponent]
public sealed partial class GrantTargetObjectiveOnGhostTakeoverComponent : Component
{
    [DataField]
    public EntityUid? TargetMind;

    [DataField(required: true)]
    public EntProtoId Objective;
}
