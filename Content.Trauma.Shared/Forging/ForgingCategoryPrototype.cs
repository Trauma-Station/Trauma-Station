// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// A forging category for the radial menu.
/// </summary>
[Prototype]
public sealed partial class ForgingCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Name shown on hover
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Icon in the radial menu
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;
}
