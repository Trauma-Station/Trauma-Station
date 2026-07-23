// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Body.Part;

[RegisterComponent, NetworkedComponent]
public sealed partial class BurnableBodyPartComponent : Component
{
    [DataField]
    public FixedPoint2 DamageThreshold = 35;

    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Heat";

    [DataField(required: true)]
    public EntProtoId<OrganComponent> BurntPart;

    [DataField]
    public SoundSpecifier? BurnSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    [DataField]
    public LocId? BurnMessage;
}
