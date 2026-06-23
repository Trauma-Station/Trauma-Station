using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning.Components;

/// <summary>
/// Stores one-time selectable ability options for a bitrunning disk.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BitrunningAbilityDiskComponent : Component
{
    /// <summary>
    /// Ability options that can be selected once.
    /// Key is shown in UI and examine text.
    /// Value is either action prototype or item prototype, based on <see cref="GrantMode"/>.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<string, EntProtoId> Options = new();

    /// <summary>
    /// Selected option key. Null until the first selection.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? SelectedOption;

    /// <summary>
    /// How selected option should be applied to avatar.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BitrunningDiskGrantMode GrantMode = BitrunningDiskGrantMode.Item;
}

[Serializable, NetSerializable]
public enum BitrunningDiskGrantMode : byte
{
    Action,
    Item,
}
