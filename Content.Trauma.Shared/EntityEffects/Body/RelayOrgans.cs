// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Applies an effect to any organs in a specific slot id.
/// The target entity must be the body.
/// </summary>
public sealed partial class RelayOrgans : EntityEffectBase<RelayOrgans>
{
    [DataField(required: true)]
    public string Slot = string.Empty;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField(required: true)]
    public LocId GuidebookText;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText, ("chance", Probability), ("slot", Slot));
}

public sealed class RelayOrgansEffectSystem : EntityEffectSystem<BodyComponent, RelayOrgans>
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<RelayOrgans> args)
    {
        var effects = args.Effect.Effects;
        var slot = args.Effect.Slot;
        foreach (var part in _body.GetBodyChildren(ent, ent.Comp))
        {
            if (_body.FindPartOrgan((part.Id, part.Component), slot) is {} organ)
                _effects.ApplyEffects(organ, effects, args.Scale);
        }
    }
}
