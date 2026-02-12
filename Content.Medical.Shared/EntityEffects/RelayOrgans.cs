// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Applies an effect to any internal organs optionally matching a whitelist.
/// The target entity must be the body.
/// </summary>
public sealed partial class RelayOrgans : EntityEffectBase<RelayOrgans>
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField]
    public LocId? GuidebookText;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => GuidebookText is {} key ? Loc.GetString(key, ("chance", Probability)) : null;
}

public sealed class RelayOrgansEffectSystem : EntityEffectSystem<BodyComponent, RelayOrgans>
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<RelayOrgans> args)
    {
        var effects = args.Effect.Effects;
        var whitelist = args.Effect.Whitelist;
        foreach (var (organ, _) in _body.GetInternalOrgans(ent.AsNullable()))
        {
            if (_whitelist.IsWhitelistFail(whitelist, organ))
                continue;

            _effects.ApplyEffects(organ, effects, args.Scale);
        }
    }
}
