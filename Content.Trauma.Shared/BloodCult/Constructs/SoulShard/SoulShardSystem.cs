// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Bible.Components;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Timing.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.BloodCult.Constructs.SoulShard;

public abstract partial class SoulShardSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private UseDelaySystem _delay = default!;
    [Dependency] private EntityQuery<SoulShardComponent> _shardQuery = default!;

    private static readonly EntProtoId PurifiedRole = "MindRolePurifiedConstruct";
    private static readonly EntProtoId SoulShard = "SoulShard";

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<SoulShardComponent> shard, ref AfterInteractUsingEvent args)
    {
        var user = args.User;

        if (shard.Comp.IsBlessed || !args.CanReach || args.Handled ||
            !TryComp(args.Used, out BibleComponent? bible) || !HasComp<BibleUserComponent>(user) ||
            !_delay.TryResetDelay(args.Used))
            return;

        args.Handled = true;

        _popup.PopupEntity("Soul shard is purified in a beam of holy light.", shard, PopupType.Medium);
        _audio.PlayPredicted(bible.HealSoundPath, user, user);
        _appearance.SetData(shard.Owner, SoulShardVisualState.Blessed, true);
        _light.SetColor(shard.Owner, shard.Comp.BlessedLightColor);
        shard.Comp.IsBlessed = true;
        shard.Comp.CultRule = null;
        Dirty(shard);

        // life is gem
        _cult.DeconvertConstruct(shard.Owner);
        if (_mind.GetMind(shard.Owner) is { } mind && mind.Valid)
        {
            _mind.ClearObjectives(mind);
            _role.MindClearRoles(mind);
            _role.MindAddRole(mind, PurifiedRole);
        }

        PurifyGhostRole(shard);
    }

    [SubscribeLocalEvent]
    private void OnShardMindAdded(Entity<SoulShardComponent> shard, ref MindAddedMessage args)
    {
        var mind = args.Mind.AsNullable();
        _role.MindClearRoles(mind);
        UpdateGlowVisuals(shard, true);

        if (shard.Comp.IsBlessed)
            _role.MindAddRole(mind, PurifiedRole);
        else if (shard.Comp.CultRule is { } rule)
            _cult.ConvertConstruct(rule, shard.Owner);
    }

    [SubscribeLocalEvent]
    private void OnShardMindRemoved(Entity<SoulShardComponent> shard, ref MindRemovedMessage args)
    {
        UpdateGlowVisuals(shard, false);
    }

    private void UpdateGlowVisuals(Entity<SoulShardComponent> shard, bool state)
    {
        _appearance.SetData(shard.Owner, SoulShardVisualState.HasMind, state);
        _light.SetEnabled(shard.Owner, state);
    }

    protected virtual void PurifyGhostRole(EntityUid uid)
    {
        // W ghost role comp still being in server
    }

    public EntityUid SpawnShard(EntityCoordinates coords, EntityUid rule)
    {
        var shard = PredictedSpawnAtPosition(SoulShard, coords);
        if (_shardQuery.TryComp(shard, out var comp))
            comp.CultRule = rule;
        return shard;
    }
}
