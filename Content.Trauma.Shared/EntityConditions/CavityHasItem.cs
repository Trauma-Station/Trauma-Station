// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Trauma.Shared.Body.Part;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target bodypart has an item in its cavity.
/// </summary>
public sealed partial class CavityHasItem : EntityConditionBase<CavityHasItem>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => string.Empty; // idc
}

public sealed partial class CavityHasItemConditionSystem : EntityConditionSystem<BodyPartCavityComponent, CavityHasItem>
{
    [Dependency] private readonly BodyPartCavitySystem _cavity = default!;

    protected override void Condition(Entity<BodyPartCavityComponent> ent, ref EntityConditionEvent<CavityHasItem> args)
    {
        args.Result = _cavity.HasItem(ent);
    }
}
