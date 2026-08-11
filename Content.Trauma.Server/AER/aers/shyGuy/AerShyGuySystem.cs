using Content.Goobstation.Server.Devil.Objectives.Components;
using Content.Goobstation.Shared.Devil;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Trauma.Shared.AER;
using Robust.Server.Player;

namespace Content.Trauma.Server.AER;

public sealed partial class AerShyGuySystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPlayerManager _player = default!;

    //adding objectives on mind added message i'm not super sure this is right but it worked, stolen from autotraitor
    [SubscribeLocalEvent]
    private void OnMindAdded(Entity<AerShyGuyComponent> ent, ref MindAddedMessage args)
    {
        if (!_player.TryGetSessionById(args.Mind.Comp.UserId, out var session))
            return;
        _mind.TryAddObjective(ent, args.Mind.Comp, "AerShyGuyObjective");
        _mind.TryAddObjective(ent, args.Mind.Comp, "AerBreachObjective");

    }
}