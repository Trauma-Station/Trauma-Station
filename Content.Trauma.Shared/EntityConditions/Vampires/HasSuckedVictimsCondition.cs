// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Shared.EntityConditions.Vampires;

/// <summary>
/// Checks whether we have sucked a certain amount of entities as vampires.
/// </summary>
public sealed partial class HasSuckedVictimsCondition : EntityConditionBase<HasSuckedVictimsCondition>
{
    /// <summary>
    /// How many entities to check against.
    /// </summary>
    [DataField(required: true)]
    public int Amount;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty; // idc
}

public sealed class HasSuckedVictimsConditionSystem : EntityConditionSystem<VampireBloodsuckingComponent, HasSuckedVictimsCondition>
{
    protected override void Condition(Entity<VampireBloodsuckingComponent> ent, ref EntityConditionEvent<HasSuckedVictimsCondition> args)
    {
        var victims = ent.Comp.ConsumedVictims.Count;
        var cond = args.Condition;

        args.Result = victims >= cond.Amount;
    }
}
