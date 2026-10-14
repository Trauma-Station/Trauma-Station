// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Bible.Components;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

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
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId ConstructRole = "MindRoleBloodCultConstruct";
    private static readonly EntProtoId PurifiedRole = "MindRolePurifiedConstruct";

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<SoulShardComponent> shard, ref InteractUsingEvent args)
    {
        if (shard.Comp.IsBlessed || !TryComp(args.Used, out BibleComponent? bible))
            return;

        var user = args.User;
        _popup.PopupEntity(Loc.GetString("bible-sizzle"), user, user);
        _audio.PlayPredicted(bible.HealSoundPath, user, user);
        _appearance.SetData(shard.Owner, SoulShardVisualState.Blessed, true);
        _light.SetColor(shard.Owner, shard.Comp.BlessedLightColor);
        shard.Comp.IsBlessed = true;
        Dirty(shard);

        // life is gem
        _cult.DeconvertConstruct(shard.Owner);
        if (_mind.GetMind(shard.Owner) is { } mind)
        {
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
        _role.MindAddRole(mind, shard.Comp.IsBlessed ? PurifiedRole : ConstructRole);
        UpdateGlowVisuals(shard, true);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<SoulShardComponent> shard, ref PlayerAttachedEvent args)
    {
        if (_cult.GetRule(shard.Owner) is not { } rule)
            return;

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
}
