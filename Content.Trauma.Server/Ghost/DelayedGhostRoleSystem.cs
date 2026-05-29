// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Roles.Components;
using Content.Shared.GameTicking;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Shared.Ghost;
using Content.Trauma.Shared.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Ghost;

public sealed partial class DelayedGhostRoleSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<DelayedGhostRoleComponent> _query = default!;

    // yeah this doesn't support persistence, tuff
    // also 2 separate buffers so there's no need for arbitrary insert, just push at the end of the buffer
    private TimedRingBuffer<EntityUid> _slow = default!;
    private TimedRingBuffer<EntityUid> _fast = default!;

    private TimeSpan _slowTime;
    private TimeSpan _fastTime;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DelayedGhostRoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        Subs.CVar(_cfg, TraumaCVars.SlowReinforcementDelay, UpdateSlowTime, true);
        Subs.CVar(_cfg, TraumaCVars.FastReinforcementDelay, UpdateFastTime, true);

        // if you can buy that many reinforcements GG
        _slow = new(64, _slowTime, _timing);
        _fast = new(16, _fastTime, _timing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_slow.PopNext(out var uid) || _fast.PopNext(out uid))
            CreateGhostRole(uid);
    }

    private void OnMapInit(Entity<DelayedGhostRoleComponent> ent, ref MapInitEvent args)
    {
        var buffer = ent.Comp.Fast ? _fast : _slow;
        if (buffer.Push(ent.Owner, out var old))
            CreateGhostRole(old); // GG
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _slow.Reset();
        _fast.Reset();
    }

    private void UpdateSlowTime(float seconds)
    {
        _slowTime = TimeSpan.FromSeconds(seconds);
        if (_slow != default)
            _slow.PopDelay = _slowTime;
    }

    private void UpdateFastTime(float seconds)
    {
        _fastTime = TimeSpan.FromSeconds(seconds);
        if (_fast != default)
            _fast.PopDelay = _slowTime;
    }

    private void CreateGhostRole(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) || !_query.TryComp(uid, out var comp))
            return;

        var role = EnsureComp<GhostRoleComponent>(uid);
        // resolving a system and updating every eui 3 times in a row award
        role.RoleName = comp.Name;
        role.RoleDescription = comp.Description;
        role.RoleRules = comp.Rules;
        role.MindRoles = comp.MindRoles;
        role.JobProto = comp.Job;
        RemComp(uid, comp);
    }
}
