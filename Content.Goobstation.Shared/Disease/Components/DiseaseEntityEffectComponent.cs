using Content.Shared.Chemistry;
using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Goobstation.Shared.Disease.Components;

/// <summary>
/// A disease effect that executes entity effects.
/// Severity from DiseaseEffectComponent automatically scales the effect strength.
/// </summary>
[RegisterComponent]
public sealed partial class DiseaseEntityEffectComponent : ScalingDiseaseEffect
{
    /// <summary>
    /// The entity effects to execute when this disease effect triggers
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = [];
}
