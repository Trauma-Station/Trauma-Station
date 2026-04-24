// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks whether the target has a specific amount of charges on them, from <see cref="LimitedChargesComponent"/>.
/// </summary>
public sealed partial class HasCharges : EntityConditionBase<HasCharges>
{
    /// <summary>
    /// How many charges we want to check against.
    /// </summary>
    [DataField(required: true)]
    public int Amount;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty; // idc
}

public sealed class HasChargesConditionSystem : EntityConditionSystem<LimitedChargesComponent, HasCharges>
{
    [Dependency] private readonly SharedChargesSystem _charges = default!;

    protected override void Condition(Entity<LimitedChargesComponent> ent, ref EntityConditionEvent<HasCharges> args)
    {
        var charges = args.Condition.Amount;

        args.Result = _charges.HasCharges(ent.AsNullable(), charges);
    }
}
