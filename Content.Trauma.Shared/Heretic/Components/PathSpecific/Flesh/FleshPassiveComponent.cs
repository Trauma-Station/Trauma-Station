// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Shared.Metabolism;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Flesh;

[RegisterComponent, NetworkedComponent]
public sealed partial class FleshPassiveComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public List<ProtoId<DamageTypePrototype>> HealthChangeImmuneDamageTypes = new()
        { "Poison", "Radiation", "Asphyxiation", "Bloodloss", "Cellular", "Caustic" };

    [DataField]
    public EntityUid? Stomach;

    [DataField]
    public float Heal = -1;

    [DataField]
    public float BloodHeal = 3f;

    [DataField]
    public float BleedHeal = -0.5f;

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
