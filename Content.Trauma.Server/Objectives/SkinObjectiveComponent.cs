// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Objectives;

/// <summary>
/// Requires that the target entity is skinned with a knife.
/// The entity can be either the mob or mind.
/// </summary>
[RegisterComponent]
public sealed partial class SkinObjectiveComponent : Component;
