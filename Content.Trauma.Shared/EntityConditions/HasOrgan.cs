// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks that the target entity has an organ matching a given whitelist.
/// </summary>
public sealed partial class HasOrgan : EntityConditionBase<HasOrgan>
{
    /// <summary>
    /// Whitelist the organ must match.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    /// <summary>
    /// Loc string explaining the organ whitelist.
    /// </summary>
    [DataField]
    public LocId? GuidebookName;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => GuidebookName is {} name
            ? Loc.GetString("entity-condition-guidebook-has-organ", ("organ", name))
            : string.Empty;
}

public sealed class HasOrganConditionSystem : EntityConditionSystem<BodyComponent, HasOrgan>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Condition(Entity<BodyComponent> ent, ref EntityConditionEvent<HasOrgan> args)
    {
        args.Result = HasOrgan(ent, args.Condition.Whitelist);
    }

    public bool HasOrgan(Entity<BodyComponent> ent, EntityWhitelist whitelist)
    {
        foreach (var (organ, _) in _body.GetBodyOrgans(ent, ent.Comp))
        {
            if (_whitelist.IsWhitelistPass(whitelist, organ))
                return true;
        }

        return false;
    }
}
