// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks the target entity against a whitelist or blacklist.
/// </summary>
public sealed partial class WhitelistCondition : EntityConditionBase<WhitelistCondition>
{
    /// <summary>
    /// A whitelist the target entity must match if non-null.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist the target entity cannot match if non-null.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Guidebook text explaining this whitelist.
    /// </summary>
    [DataField(required: true)]
    public LocId GuidebookText;

    /// <summary>
    /// Whether it should also check if mind entity passes for whitelist
    /// </summary>
    [DataField]
    public bool CheckMind;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString(GuidebookText);
}

public sealed class WhitelistConditionSystem : EntityConditionSystem<MetaDataComponent, WhitelistCondition>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;


    protected override void Condition(Entity<MetaDataComponent> ent, ref EntityConditionEvent<WhitelistCondition> args)
    {
        var cond = args.Condition;
        args.Result = _whitelist.CheckBoth(ent, blacklist: cond.Blacklist, whitelist: cond.Whitelist);

        if (args.Result || !cond.CheckMind)
            return;

        args.Result = _mind.TryGetMind(ent, out var mind, out _) &&
                      _whitelist.CheckBoth(mind, blacklist: cond.Blacklist, whitelist: cond.Whitelist);
    }
}
