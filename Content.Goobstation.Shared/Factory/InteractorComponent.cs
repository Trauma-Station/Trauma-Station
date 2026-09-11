// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;

namespace Content.Goobstation.Shared.Factory;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedInteractorSystem))]
[AutoGenerateComponentState]
public sealed partial class InteractorComponent : Component
{
    [DataField]
    public string ToolContainerId = "interactor_tool";

    /// <summary>
    /// Signal port to toggle or enable/disable <see cref="AltInteract"/>.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> AltInteractPort = "AltInteract";

    /// <summary>
    /// Signal port to toggle or enable/disable <see cref="UseInHand"/>.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> UseInHandPort = "UseInHand";

    /// <summary>
    /// Signal port to toggle or enable/disable <see cref="HarmMode"/>.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> HarmModePort = "HarmMode";

    /// <summary>
    /// Signal port to toggle or enable/disable <see cref="Locked"/>.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> LockedPort = "ToolLocked";

    /// <summary>
    /// Whether to use alt interaction, i.e. use the highest priority verb on the target entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AltInteract;

    /// <summary>
    /// Whether to use the item inhand, ignores target entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseInHand;

    /// <summary>
    /// If the interactor should act as if it is in harmmode and should hit targets with its held item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HarmMode;

    /// <summary>
    /// Prevents picking up or dropping its item in any way while locked.
    /// Useful for interacting with storage and you don't want to insert the item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked;
}

[Serializable, NetSerializable]
public enum InteractorVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum InteractorLayers : byte
{
    Hand,
    Powered
}

[Serializable, NetSerializable]
public enum InteractorState : byte
{
    // Inactive with no tool
    Empty,
    // Inactive with a tool
    Inactive,
    // Active, with or without a tool
    Active
}
