// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Condition that requires the target entity to be the user.
/// </summary>
public sealed partial class IsUser : EntityConditionBase<IsUser>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class IsUserConditionSystem : EntityConditionSystem<MetaDataComponent, IsUser>
{
    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<IsUser> args)
    {
        args.Result = args.SourceEnt == ent.Owner;
    }
}
