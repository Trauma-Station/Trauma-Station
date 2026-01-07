// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.EntityEffects;

public sealed class MakeTraitorEffectSystem : EntityEffectSystem<ActorComponent, MakeTraitor>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    private static readonly EntProtoId DefaultTraitorRule = "Traitor";

    protected override void Effect(Entity<ActorComponent> ent, ref EntityEffectEvent<MakeTraitor> args)
    {
        var session = ent.Comp.PlayerSession;
        _antag.ForceMakeAntag<TraitorRuleComponent>(session, DefaultTraitorRule);
    }
}
