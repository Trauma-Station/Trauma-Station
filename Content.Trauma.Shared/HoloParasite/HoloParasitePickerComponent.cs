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
    public string Caption = default!;

    [DataField]
    public string? Synopsis;

    [DataField]
    public string? Lore;
}

[Serializable, NetSerializable]
public enum HoloParasitePickerUiKey : byte
{
    Key
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