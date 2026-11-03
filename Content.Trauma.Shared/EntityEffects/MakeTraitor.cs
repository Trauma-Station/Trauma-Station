// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.EntityEffects;
using Content.Shared.GameTicking.Rules.Components;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Makes the target entity a traitor, if it has a player controlling it.
/// </summary>
public sealed partial class MakeTraitor : EntityEffectBase<MakeTraitor>
{
    [DataField]
    public EntProtoId Rule = "Traitor";

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-make-traitor", ("chance", Probability));
}

public sealed partial class MakeTraitorEffectSystem : EntityEffectSystem<ActorComponent, MakeTraitor>
{
    [Dependency] private AntagSelectionSystem _antag = default!;

    protected override void Effect(Entity<ActorComponent> ent, ref EntityEffectEvent<MakeTraitor> args)
    {
        var session = ent.Comp.PlayerSession;
        _antag.ForceMakeAntag<TraitorRuleComponent>(session, args.Effect.Rule);
    }
}
