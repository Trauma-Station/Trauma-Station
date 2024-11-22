// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Bible.Components;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.BloodCult.Constructs.SoulShard;

public sealed partial class SoulShardSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnActivate(Entity<SoulShardComponent> shard, ref ActivateInWorldEvent args)
    {
        if (!_mind.TryGetMind(shard, out var mindId, out var mind))
            return;

        var proto = shard.Comp.PurifiedShadeProto;
        if (!shard.Comp.IsBlessed)
        {
            if (!_cult.IsCultist(args.User))
                return;

            proto = shard.Comp.ShadeProto;
        }

        if (shard.Comp.ShadeUid is {} shade)
            DespawnShade(shard, shade);
        else
            SpawnShade(shard, proto, (mindId, mind));
    }

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
    }

    [SubscribeLocalEvent]
    private void OnShardMindAdded(Entity<SoulShardComponent> shard, ref MindAddedMessage args)
    {
        // TODO: ummmmmmmmmmmmmmmm this isnt every antag
        _role.MindRemoveRole<TraitorRoleComponent>(args.Mind.AsNullable());
        UpdateGlowVisuals(shard, true);
    }

    [SubscribeLocalEvent]
    private void OnShardMindRemoved(Entity<SoulShardComponent> shard, ref MindRemovedMessage args)
    {
        UpdateGlowVisuals(shard, false);
    }

    private void SpawnShade(Entity<SoulShardComponent> shard, EntProtoId proto, Entity<MindComponent?> mind)
    {
        var coords = Transform(shard).Coordinates;
        var shadeUid = PredictedSpawnAtPosition(proto, coords);
        _mind.TransferTo(mind, shadeUid);
        _mind.UnVisit(mind);
        shard.Comp.ShadeUid = shadeUid;
        Dirty(shard);
    }

    private void DespawnShade(Entity<SoulShardComponent> shard, EntityUid shade)
    {
        if (!_mind.TryGetMind(shade, out var mindId, out var mind))
        {
            _mind.TransferTo(mindId, shard, mind: mind);
            _mind.UnVisit(mindId, mind: mind);
        }

        PredictedDel(shade);
        shard.Comp.ShadeUid = null;
        Dirty(shard);
    }

    private void UpdateGlowVisuals(Entity<SoulShardComponent> shard, bool state)
    {
        _appearance.SetData(shard.Owner, SoulShardVisualState.HasMind, state);
        _light.SetEnabled(shard.Owner, state);
    }
}
