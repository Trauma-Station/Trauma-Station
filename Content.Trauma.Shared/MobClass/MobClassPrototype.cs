// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.MobClass;

/// <summary>
/// Holds data about the class
/// </summary>
[Prototype]
public sealed partial class MobClassPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of this class. Used for displaying it in UI.
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>
    /// A description of the class.
    /// </summary>
    [DataField]
    public string Description = string.Empty;

    /// <summary>
    /// Effects to apply when a user has selected this class.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// The icon that will appear in the UI.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;
}
