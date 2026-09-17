// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.HoloParasite;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HoloParasitePickerComponent : Component
{
    [DataField(required: true)]
    public List<HoloParasiteVariant> Variants = new();

    [DataField, AutoNetworkedField]
    public EntProtoId? ChosenVariant;

    [DataField, AutoNetworkedField]
    public EntityUid? HostTarget;
}

[DataDefinition]
public partial struct HoloParasiteVariant : IEquatable<HoloParasiteVariant>
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField(required: true)]
    public string Caption = default!;

    [DataField]
    public string? Synopsis;

    [DataField]
    public string? Lore;

    public static implicit operator HoloParasiteVariant(string prototypeId) =>
        new() { Prototype = prototypeId };

    public bool Equals(HoloParasiteVariant other) => Prototype == other.Prototype;

    public override bool Equals(object? obj) => obj is HoloParasiteVariant other && Equals(other);

    public override int GetHashCode() => Prototype.GetHashCode();
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