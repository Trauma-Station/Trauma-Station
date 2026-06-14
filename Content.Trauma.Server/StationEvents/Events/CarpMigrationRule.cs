// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Trauma.Server.StationEvents.Components;
using Content.Trauma.Shared.Areas;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Trauma.Server.StationEvents.Events;

public sealed partial class CarpMigrationRule : StationEventSystem<CarpMigrationRuleComponent>
{
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private NPCSystem _npc = default!;

    private List<Entity<TransformComponent>> _areas = new();

    protected override void Started(EntityUid uid, CarpMigrationRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (CompOrNull<SpaceSpawnRuleComponent>(uid)?.Coords is not {} spawnPos)
        {
            Log.Error($"Event {ToPrettyString(uid)} had no SpaceSpawnRule or picked location!");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // pick a random hallway for the carp to navigate to, they should break windows to try get inside
        _areas.Clear();
        _area.AddOpenAreas<CarpMigrationTargetComponent>(spawnPos.MapId, _areas, _ => true);
        if (_areas.Count == 0)
        {
            ForceEndSelf(uid, gameRule);
            return;
        }
        var target = RobustRandom.Pick(_areas).Comp.Coordinates;

        // spawn all the carp and make them navigate to the target area
        var count = RobustRandom.Next(comp.Min, comp.Max);
        for (int i = 0; i < count; i++)
        {
            var carp = Spawn(comp.Proto, spawnPos);
            _npc.SetBlackboard(carp, NPCBlackboard.FollowTarget, target);
        }

        // announce it
        var filter = Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame);
        ChatSystem.DispatchFilteredAnnouncement(filter,
            "Unknown biological entities have been detected near the station, please stand by.",
            sender: "Lifesign Alert",
            colorOverride: Color.Gold);
    }
}
