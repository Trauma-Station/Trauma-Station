// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Tools;

/// <summary>
/// WelderComponent but debloated. Or so I think. Those imp coders are weird sometimes.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class SolutionRefuelComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Name = "tank";

    /// <summary>
    ///     Name of <see cref="FuelSolution"/>.
    /// </summary>
    [DataField]
    public string FuelSolutionName = "Welder";

    /// <summary>
    ///     Reagent that will be used as fuel.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> FuelReagent = "WeldingFuel";

    /// <summary>
    ///     Sound played when refilling the welder.
    /// </summary>
    [DataField]
    public SoundSpecifier WelderRefill = new SoundPathSpecifier("/Audio/Effects/refill.ogg");
}
