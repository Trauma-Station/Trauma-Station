// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

[RegisterComponent]
public sealed partial class RustRequiresPathStageComponent : Component
{
    /// <summary>
    /// If rust heretic path stage is less than this - they won't be able to rust this surface
    /// </summary>
    [DataField]
    public int PathStage = 2;
}
