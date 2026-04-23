using System.Linq;
using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Actions;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Timing;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Rituals;
using Content.Trauma.Shared.Heretic.Ui;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Flesh;

public sealed class FleshGraspSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHereticRitualSystem _ritual = default!;
    [Dependency] private readonly TouchSpellSystem _touchSpell = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [Dependency] private readonly EntityQuery<DamageOverTimeComponent> _mimicQuery = default!;
    [Dependency] private readonly EntityQuery<GhoulComponent> _ghoulQuery = default!;

    private static readonly EntProtoId MansusGraspAction = "ActionHereticMansusGrasp";

    private static readonly EntityWhitelist GraspWhitelist = new()
    {
        Components = ["FleshGrasp"],
    };

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<HereticRitualRuneComponent>(HereticGhoulRecallKey.Key,
            subs =>
            {
                subs.Event<HereticGhoulRecallMessage>(OnRecall);
            });
    }

    private void OnRecall(Entity<HereticRitualRuneComponent> ent, ref HereticGhoulRecallMessage args)
    {
        if (!_heretic.TryGetHereticComponent(args.Actor, out var heretic, out var mind) ||
            !HasComp<FleshHereticMindComponent>(mind))
            return;

        if (!TryGetEntity(args.Ghoul, out var ghoul) || !heretic.Minions.Contains(ghoul.Value))
        {
            OpenUi(ent, (mind, heretic), args.Actor);
            return;
        }

        if (_touchSpell.FindTouchSpell(args.Actor, GraspWhitelist) is not { } touchSpell)
        {
            OpenUi(ent, (mind, heretic), args.Actor);
            return;
        }

        if (!_delay.TryResetDelay(touchSpell, true))
        {
            OpenUi(ent, (mind, heretic), args.Actor);
            return;
        }

        if (!_actions.TryGetActionById(mind, MansusGraspAction, out var action))
        {
            OpenUi(ent, (mind, heretic), args.Actor);
            return;
        }

        _actions.SetIfBiggerCooldown(action.Value.AsNullable(), TimeSpan.FromSeconds(1.5));

        if (_net.IsServer)
        {
            _ritual.RitualSuccess(ent, args.Actor, false);
            _touchSpell.InvokeTouchSpell(touchSpell, args.Actor, TimeSpan.Zero, false);
        }

        _pulling.StopAllPulls(ghoul.Value);
        _transform.SetMapCoordinates(ghoul.Value, _transform.GetMapCoordinates(ent));

        OpenUi(ent, (mind, heretic), args.Actor);
    }

    public void OpenUi(EntityUid rune, Entity<HereticComponent> heretic, EntityUid user)
    {
        var coords = _transform.GetMapCoordinates(rune);
        var list = heretic.Comp.Minions
            .Where(x => Exists(x) && !Paused(x) && !_mimicQuery.HasComp(x) && _ghoulQuery.HasComp(x))
            .Select(x => new GhoulRecallData(GetNetEntity(x), Name(x), GetDist(x)))
            .ToList();
        _ui.TryOpenUi(rune, HereticGhoulRecallKey.Key, user);
        _ui.SetUiState(rune, HereticGhoulRecallKey.Key, new HereticGhoulRecallUiState(list));

        return;

        float? GetDist(EntityUid uid)
        {
            var ourCoords = _transform.GetMapCoordinates(uid);
            if (ourCoords.MapId != coords.MapId)
                return null;
            return (ourCoords.Position - coords.Position).Length();
        }
    }
}
