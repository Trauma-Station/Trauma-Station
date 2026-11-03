using Content.Shared.EntityConditions;

namespace Content.Shared.Chemistry.Reagent;

public sealed partial class ReagentPrototype
{
    /// <summary>
    /// If false, reagent will be put back into the puddle after slip on puddle.
    /// </summary>
    [DataField]
    public bool SticksToSkin = true;
}

public sealed partial class ReagentEffectsEntry
{
    /// <summary>
    /// Group conditions that apply before individual effects.
    /// </summary>
    [DataField]
    public EntityCondition[] Conditions = [];
}
