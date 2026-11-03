// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.RadialSelector;

[NetSerializable, Serializable]
public enum RadialSelectorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RadialSelectorState(List<RadialSelectorEntry> entries) : BoundUserInterfaceState
{
    public List<RadialSelectorEntry> Entries = entries;
}

[Serializable, NetSerializable]
public sealed class RadialSelectorSelectedMessage(string selectedItem) : BoundUserInterfaceMessage
{
    public readonly string SelectedItem = selectedItem;
}

[DataDefinition, Serializable, NetSerializable]
public partial record struct RadialSelectorEntry
{
    [DataField]
    public string? Prototype;
    [DataField]
    public SpriteSpecifier? Icon;
    [ViewVariables]
    public NetEntity? IconEntity;
    [DataField]
    public RadialSelectorCategory? Category;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RadialSelectorCategory
{
    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    [DataField(required: true)]
    public List<RadialSelectorEntry> Entries = default!;
}
