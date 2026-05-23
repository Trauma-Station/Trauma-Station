// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.Side.Carvings;

[RegisterComponent, NetworkedComponent]
public sealed partial class CarvingKnifeComponent : Component
{
    [DataField]
    public List<EntProtoId> Carvings = new();

    [DataField(serverOnly: true)]
    public List<EntityUid> DrawnRunes = new();

    [DataField]
    public int MaxRuneAmount = 3;

    [DataField]
    public TimeSpan RuneDrawTime = TimeSpan.FromSeconds(3f);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/sheath.ogg");

    [DataField]
    public EntProtoId RunebreakAction = "ActionRunebreak";

    [DataField]
    public EntityUid? RunebreakActionEntity;
}

[Serializable, NetSerializable]
public sealed class RuneCarvingSelectedMessage(EntProtoId protoId) : BoundUserInterfaceMessage
{
    public EntProtoId ProtoId { get; } = protoId;
}

[Serializable, NetSerializable]
public enum CarvingKnifeUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class CarveRuneDoAfterEvent(EntProtoId carving) : DoAfterEvent
{
    public EntProtoId Carving = carving;

    public CarveRuneDoAfterEvent() : this(default) { }

    public override DoAfterEvent Clone() => this;
}

public sealed partial class DeleteAllCarvingsEvent : InstantActionEvent;
