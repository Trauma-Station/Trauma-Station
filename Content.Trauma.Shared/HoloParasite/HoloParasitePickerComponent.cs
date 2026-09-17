// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.HoloParasite;

[RegisterComponent]
public sealed partial class HoloParasitePickerComponent : Component
{
    [DataField(required: true)]
    public List<HoloParasiteVariant> Variants = new();

    [DataField]
    public EntProtoId? ChosenVariant;

    public List<EntityUid> DetectedHosts = new();
}

[DataDefinition]
public sealed partial class HoloParasiteVariant
{
    [DataField(required: true)]
    public EntProtoId Prototype = default!;

    [DataField(required: true)]
    public LocId Caption = default!;

    [DataField]
    public LocId? Synopsis;

    [DataField]
    public LocId? Lore;
}

[Serializable, NetSerializable]
public enum HoloParasitePickerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class HoloParasitePickerState : BoundUserInterfaceState
{
    public List<HoloParasiteChoice> Choices;
    public int StartIndex;

    public HoloParasitePickerState(List<HoloParasiteChoice> choices, int startIndex)
    {
        Choices = choices;
        StartIndex = startIndex;
    }
}

[Serializable, NetSerializable]
public sealed class HoloParasiteChoice
{
    public string Caption;
    public string? Synopsis;
    public string? Lore;
    public string ProtoId;

    public HoloParasiteChoice(string caption, string? synopsis, string? lore, string protoId)
    {
        Caption = caption;
        Synopsis = synopsis;
        Lore = lore;
        ProtoId = protoId;
    }
}

[Serializable, NetSerializable]
public sealed class HoloParasitePickMessage : BoundUserInterfaceMessage
{
    public string ChosenProto;

    public HoloParasitePickMessage(string chosenProto)
    {
        ChosenProto = chosenProto;
    }
}
