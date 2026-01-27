// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.FixedPoint;
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
    /// How long to wait after injecting main chemicals to inject comedown chemicals.
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
    /// Quantity for each reagent to add.
    /// </summary>
    public FixedPoint2 Quantity;
    public LocId Popup;
}

public sealed partial class InjectChemicalsActionEvent : InstantActionEvent;
