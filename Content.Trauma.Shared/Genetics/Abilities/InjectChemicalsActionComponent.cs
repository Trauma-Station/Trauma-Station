using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Adds reagents to the user's bloodstream, then after a comedown period adds different reagents.
/// This must be added to an action entity, with <c>raiseOnAction: true</c>
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(InjectChemicalsActionSystem))]
[AutoGenerateComponentPause]
public sealed partial class InjectChemicalsActionComponent : Component
{
    /// <summary>
    /// Chemicals to inject immediately on use
    /// </summary>
    [DataField(required: true)]
    public InjectionConfig Main = default!;

    /// <summary>
    /// Chemicals to inject after <see cref="ComedownDelay"/>.
    /// </summary>
    [DataField(required: true)]
    public InjectionConfig Comedown = default!;

    /// <summary>
    /// Base comedown delay that can be modified by chromosomes.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan ComedownDelay;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextComedown;
}

[DataRecord]
public partial record struct InjectionConfig
{
    /// <summary>
    /// Each reagent to inject, using <see cref="BaseQuantity"/>.
    /// </summary>
    public List<ProtoId<ReagentPrototype>> Reagents;
    /// <summary>
    /// Quantity that can be scaled up/down depending on chromosomes.
    /// </summary>
    public FixedPoint2 BaseQuantity;
    public LocId Popup;
}

public sealed partial class InjectChemicalsActionEvent : InstantActionEvent;
