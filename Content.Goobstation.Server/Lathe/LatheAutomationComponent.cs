// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DeviceLinking;
using Content.Shared.Research.Prototypes;

namespace Content.Goobstation.Server.Lathe;

/// <summary>
/// Lets a lathe produce the last made recipe, controlled by signal ports.
/// The ports must be added by something else e.g. AutomationSlots
/// </summary>
[RegisterComponent, Access(typeof(LatheAutomationSystem))]
public sealed partial class LatheAutomationComponent : Component
{
    [ViewVariables]
    public LatheRecipePrototype? LastRecipe;

    /// <summary>
    /// How many times to try produce <see cref="LastRecipe"/> when <see cref="PrintPort"/> is invoked.
    /// </summary>
    [DataField]
    public int Quantity = 1;

    [DataField]
    public ProtoId<SinkPortPrototype> PrintPort = "LathePrint";

    [DataField]
    public ProtoId<SinkPortPrototype> SetRecipePort = "LatheSetRecipe";

    [DataField]
    public ProtoId<SinkPortPrototype> QuantityPort = "LatheQuantity";

    [DataField]
    public ProtoId<SourcePortPrototype> CurrentRecipePort = "LatheCurrentRecipe";
}
