// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Salvage;
using Content.Shared.GameTicking;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Salvage;

/// <summary>
/// Stores total claimed mining points in a round for salv objectives.
/// The count is tied to the user id and persists across ghost roles etc.
/// </summary>
public sealed class MiningRewardsSystem : EntitySystem
{
    [Dependency] private readonly EntityQuery<ActorComponent> _actorQuery = default!;

    // TODO: put the dict on a round entity wsci
    private Dictionary<NetUserId, int> PointsPerPlayer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiningPointsClaimedEvent>(OnPointsClaimed);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnPointsClaimed(ref MiningPointsClaimedEvent args)
    {
        if (!_actorQuery.TryComp(args.User, out var actor))
            return;

        var id = actor.PlayerSession.UserId;
        PointsPerPlayer[id] = GetPointsClaimed(id) + args.Points;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        PointsPerPlayer.Clear();
    }

    /// <summary>
    /// Get the number of points a player has claimed this round, defaulting to 0.
    /// </summary>
    public int GetPointsClaimed(NetUserId id)
        => PointsPerPlayer.GetValueOrDefault(id);
}
