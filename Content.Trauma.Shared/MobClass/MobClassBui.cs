// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.MobClass;

/// <summary>
/// For use with actions, opens the class selector ui.
/// </summary>
public sealed partial class OpenClassSelectorUiEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class MobClassSelectedMessage(ProtoId<MobClassPrototype> classProto) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The class we selected to specialize in.
    /// </summary>
    public ProtoId<MobClassPrototype> ClassProto = classProto;
}

[Serializable, NetSerializable]
public sealed class MobClassState(ProtoId<MobClassGroupPrototype> groupProto) : BoundUserInterfaceState
{
    /// <summary>
    /// The classes to display to the user.
    /// </summary>
    public ProtoId<MobClassGroupPrototype> Group = groupProto;
}

[Serializable, NetSerializable]
public enum MobClassUiKey : byte
{
    Key
}
