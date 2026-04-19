// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Kudzu;

/// <summary>
/// Counts as foliage, and therefore gets a lower layer if the entity seeing them has the FoliageIgnoringVision Component.
/// </summary>
[RegisterComponent,NetworkedComponent]
public sealed partial class HideableFoliageComponent : Component;
