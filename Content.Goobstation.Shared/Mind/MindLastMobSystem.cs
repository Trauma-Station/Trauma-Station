// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Trauma.Common.Mind;

namespace Content.Goobstation.Shared.Mind;

public sealed partial class MindLastMobSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityQuery<MindLastMobComponent> _query = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;

    [SubscribeLocalEvent]
    private void OnMindGotAdded(Entity<MindLastMobComponent> ent, ref MindGotAddedEvent args)
    {
        if (!_mobQuery.HasComp(args.Container) || TerminatingOrDeleted(args.Container))
            return;

        ent.Comp.LastMob = args.Container;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnMindContainerShutdown(Entity<MindContainerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Mind is not { } mind ||
            !_query.TryComp(mind, out var comp) ||
            comp.LastMob != ent.Owner)
            return;

        comp.LastMob = null;
        Dirty(mind, comp);
    }

    [SubscribeLocalEvent]
    private void OnGetPlayerInfo(Entity<MindLastMobComponent> ent, ref RoundEndGetPlayerInfoEvent args)
    {
        if (ent.Comp.LastMob is not { } mob || TerminatingOrDeleted(mob))
            return;

        if (_mobQuery.TryComp(mob, out var state))
            args.MobState = (byte) state.CurrentState;

        // TODO: store a thing on the mind when gibbing/cremating/singuloing someone for special displaying
        foreach (var (group, amount) in _damage.GetDamagePerGroup(mob))
        {
            args.DamagePerGroup[group] = amount;
        }
    }
}
