// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Shared.EntityConditions.Vampires;

/// <summary>
/// Checks whether the target has a specific amount of usable blood on them, from <see cref="VampireComponent"/>.
/// </summary>
public sealed partial class HasVampireBlood : EntityConditionBase<HasVampireBlood>
{
    /// <summary>
    /// How much usable blood we want to check against.
    /// </summary>
    [DataField(required: true)]
    public int Amount;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty; // idc
}

public sealed partial class HasVampireBloodConditionSystem : EntityConditionSystem<VampireComponent, HasVampireBlood>
{
    [Dependency] private VampireSystem _vampire = default!;

    protected override void Condition(Entity<VampireComponent> ent, ref EntityConditionEvent<HasVampireBlood> args)
    {
        var blood = args.Condition.Amount;

        args.Result = _vampire.HasUsableBlood(ent.AsNullable(), blood);
    }
}
