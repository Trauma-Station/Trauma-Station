using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Antag.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag;

public sealed partial class ServerAntagSelectionSystem
{
    [Dependency] private EntityQuery<AntagSelectionComponent> _query = default!;

    public override Entity<AntagSelectionComponent>? ForceGetGameRuleEnt([ForbidLiteral] EntProtoId id, [ForbidLiteral] CompName comp)
    {
        var type = Factory.GetRegistration(comp).Type;
        var query = EntityManager.AllEntityQueryEnumerator(type);
        while (query.MoveNext(out var uid, out _))
        {
            if (_query.TryComp(uid, out var ontag))
                return (uid, ontag);
        }

        if (GameTicker.AddGameRule(id) is not { } rule)
            return null;

        RemComp<LoadMapRuleComponent>(rule);
        var antag = Comp<AntagSelectionComponent>(rule);
        antag.AssignmentHandled = true; // don't do normal selection.
        GameTicker.StartGameRule(rule.AsNullable());
        return (rule, antag);
    }
}
