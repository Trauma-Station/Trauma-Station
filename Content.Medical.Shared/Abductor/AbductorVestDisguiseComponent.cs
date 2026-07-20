// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Medical.Shared.Abductor;

[RegisterComponent]
public sealed partial class AbductorVestDisguiseComponent : Component;

[RegisterComponent]
public sealed partial class AbductorDisguiseStateComponent : Component
{
    [DataField]
    public EntityUid? RevertAction;

    [DataField]
    public string? OriginalName;

    [DataField]
    public Dictionary<EntityUid, DisguiseData>? OriginalOrganData;

    [DataField]
    public HumanoidCharacterProfile? OriginalProfile;
}

public sealed partial class DisguiseRevertEvent : InstantActionEvent
{
    [DataField]
    public bool RaiseRenameEvents;
}

[Serializable, NetSerializable, DataRecord]
public partial record struct DisguiseData(
    PrototypeLayerData PrototypeLayerData,
    Dictionary<Sex, string>? SexStateOverrides,
    OrganMarkingData MarkingData,
    HashSet<Enum> HideableLayers,
    Dictionary<Enum, HashSet<Enum>> DependentHidingLayers);
