using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Salvage;
using Content.Trauma.Common.ServerCurrency;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Trauma.Server.ServerCurrency;

/// <summary>
/// Gives players money when they claim points, up to a limit.
/// The limit is tied to the user id and persists across ghost roles etc.
/// </summary>
public sealed class MiningRewardsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private EntityQuery<ActorComponent> _actorQuery;

    // TODO: put the dict on a round entity wsci
    private Dictionary<NetUserId, int> PointsPerPlayer = new();
    private int _limit, _ratio;

    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();

        SubscribeLocalEvent<MiningPointsClaimedEvent>(OnPointsClaimed);
        SubscribeLocalEvent<ModifyCurrencyEvent>(OnModifyCurrency);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        Subs.CVar(_cfg, TraumaCVars.MiningRewardLimit, x => _limit = x, true);
        Subs.CVar(_cfg, TraumaCVars.MiningRewardRatio, x => _ratio = x, true);
    }

    private void OnPointsClaimed(ref MiningPointsClaimedEvent args)
    {
        if (!_actorQuery.TryComp(args.User, out var actor))
            return;

        var id = actor.PlayerSession.UserId;
        PointsPerPlayer[id] = GetPointsClaimed(id) + args.Points;
    }

    private void OnModifyCurrency(ref ModifyCurrencyEvent args)
    {
        var id = args.Session.UserId;
        var gained = GetPointsClaimed(id) / _ratio;
        var money = Math.Min(gained, _limit);
        Log.Debug($"User {args.Session.Name} got {money} currency from mining this round");
        args.Money += money;
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

    /// <summary>
    /// The number of points after which you will receive no money.
    /// </summary>
    public int PointsQuota => _limit * _ratio;
}
