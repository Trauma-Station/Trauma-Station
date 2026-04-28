using Content.Server.EUI;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Trauma.Server.Ghost;
using Content.Trauma.Shared.Ghost;
using Robust.Server.Player;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed class GhostroleAlertSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostroleAlertComponent, GameRuleAddedEvent>(OnRuleAdded);
    }

    private void OnRuleAdded(Entity<GhostroleAlertComponent> ent, ref GameRuleAddedEvent args)
    {
        var query = EntityQueryEnumerator<GhostComponent>();
        while (query.MoveNext(out var ghostUid, out _))
        {
            _player.TryGetSessionByEntity(ghostUid, out var session);
            _euiMan.OpenEui(new GhostroleAlertEui(), session!);
        }
    }
}
