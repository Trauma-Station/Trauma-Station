// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Components;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Goobstation.Shared.Possession;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class PossessedComponent : Component
{
    [DataField]
    public EntityUid OriginalMindId;

    [DataField]
    public EntityUid OriginalEntity;

    [DataField]
    public EntityUid PossessorMindId;

    [DataField]
    public EntityUid PossessorOriginalEntity;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan PossessionEndTime;

    [DataField]
    public bool WasPacified;

    [DataField]
    public bool WasWeakToHoly;

    [ViewVariables]
    public Container PossessedContainer;

    [DataField]
    public EntProtoId<ActionComponent> EndPossessionAction = "ActionEndPossession";

    [DataField]
    public bool HideActions = true;

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField] // not networked because of engine bug
    public EntityUid[] HiddenActions = default!;

    [DataField]
    public bool PolymorphEntity = true;

    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph = "ShadowJauntPermanent";

    [DataField]
    public SoundPathSpecifier PossessionSound = new("/Audio/_Goobstation/Effects/bone_crack.ogg");
}
