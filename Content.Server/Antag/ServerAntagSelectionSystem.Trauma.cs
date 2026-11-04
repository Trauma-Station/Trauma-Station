using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Antag.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag;

public sealed partial class ServerAntagSelectionSystem
{
    [Dependency] private EntityQuery<AntagSelectionComponent> _query = default!;

    public override Entity<AntagSelectionComponent>? ForceGetGameRuleEnt([ForbidLiteral] EntProtoId id, [ForbidLiteral] CompName comp)
    {
        if (FindRule(comp) is { } existing)
            return existing;

        if (GameTicker.AddGameRule(id) is not { } rule)
            return null;

        RemComp<LoadMapRuleComponent>(rule);
        var antag = Comp<AntagSelectionComponent>(rule);
        antag.AssignmentHandled = true; // don't do normal selection.
        GameTicker.StartGameRule(rule.AsNullable());
        return (rule, antag);
    }
}
