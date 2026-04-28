// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Metabolism;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Flesh;

[RegisterComponent, NetworkedComponent]
public sealed partial class FleshPassiveComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public EntityUid? Stomach;

    [DataField]
    public ProtoId<ReagentPrototype> ReagentId = "RawFlesh";

    [DataField]
    public float ReagentMultiplier = 0.1f;

    [DataField]
    public float OrganMultiplier = 2f;

    [DataField]
    public float BodyPartMultiplier = 5f;

    [DataField]
    public float MobMultiplier = 5f;

    [DataField]
    public float BrainMultiplier = 2f;

    [DataField]
    public float HumanMultiplier = 2f;

    [DataField]
    public float AscensionMultiplier = 2f;

    // Prevents heretics from vomiting when consuming flesh and other stuff
    [DataField]
    public ProtoId<MetabolizerTypePrototype> FleshMetabolizer = "Vox";
}
