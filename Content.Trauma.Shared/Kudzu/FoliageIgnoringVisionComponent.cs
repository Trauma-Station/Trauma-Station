// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Kudzu;

/// <summary>
/// Makes "Foliage" with the IsFoliage Component render lower for the entity with this Component.
/// </summary>
[RegisterComponent]
public sealed partial class FoliageIgnoringVisionComponent : Component;
