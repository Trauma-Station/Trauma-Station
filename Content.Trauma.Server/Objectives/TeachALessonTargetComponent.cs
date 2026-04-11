// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Objectives;

/// <summary>
/// Marker component for the target of Teach a lesson Objective
/// Holds HashSet of entities with this objective
/// </summary>

[RegisterComponent ]
public sealed partial class TeachALessonTargetComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Teachers = new HashSet<EntityUid>();
}
