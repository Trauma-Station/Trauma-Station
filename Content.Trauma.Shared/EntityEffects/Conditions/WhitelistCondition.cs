using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects.Conditions;

/// <summary>
/// Checks the target entity against a whitelist and blacklist.
/// </summary>
public sealed partial class WhitelistCondition : EntityEffectCondition
{
    private EntityWhitelistSystem? _whitelist;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Guidebook text for reagents.
    /// This should describe the whitelist/blacklist in a player-readable fashion.
    /// </summary>
    [DataField(required: true)]
    public LocId GuidebookText;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        _whitelist ??= args.EntityManager.System<EntityWhitelistSystem>();

        return _whitelist.CheckBoth(args.TargetEntity, Blacklist, Whitelist);
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
        => Loc.GetString(GuidebookText);
}
