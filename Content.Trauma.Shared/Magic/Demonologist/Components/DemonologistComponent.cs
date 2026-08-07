// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.Magic.Demonologist.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DemonologistComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "DemonologistFaction";

    [DataField]
    public EntProtoId CombustionActionPrototype = "ActionCombustion";

    [DataField]
    public EntProtoId BindApprenticeActionPrototype = "ActionBindApprentice";

    [DataField]
    public EntProtoId BloodBoilActionPrototype = "ActionBloodBoil";

    [DataField]
    public EntProtoId CursedAccessActionPrototype = "ActionCursedAccess";

    [DataField]
    public EntityUid? CursedAccessAction;

    [DataField]
    public EntityUid? BloodBoilAction;

    [DataField]
    public EntityUid? BindApprenticeAction;

    [DataField]
    public EntityUid? CombustionAction;
}
