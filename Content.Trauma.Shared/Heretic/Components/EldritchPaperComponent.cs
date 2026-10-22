// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components;

/// <summary>
/// Heretic paper that stores an item inside, created using <see cref="EldritchPaperConverterComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EldritchPaperComponent : Component
{
    /// <summary>
    /// Container slot where items are put
    /// </summary>
    [DataField]
    public string Slot = "item";

    /// <summary>
    /// Visual scale for item overlay on the paper
    /// </summary>
    [DataField]
    public Vector2 ItemScale = new Vector2(0.375f);

    /// <summary>
    /// Used to add/clear layers of item stored when appearance changes
    /// </summary>
    [ViewVariables]
    public int AddedLayers;
}

[Serializable, NetSerializable]
public enum EldritchPaperVisuals : byte
{
    Item,
}
