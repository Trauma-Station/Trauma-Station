// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Trauma.Shared.Body.Markings;

public sealed partial class ModifyMarkingsStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    [Dependency] private EntityQuery<ModifyMarkingStatusEffectComponent> _markingStatusQuery = default!;

    [SubscribeLocalEvent]
    private void OnOrganRemove(Entity<StatusEffectContainerComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (args.Organ.Comp.Category is not { } category)
            return;

        if (ent.Comp.ActiveStatusEffects?.ContainedEntities is not { } list)
            return;

        foreach (var effect in list)
        {
            if (!_markingStatusQuery.TryComp(effect, out var markingStatus) || markingStatus.Organ != category)
                continue;

            ToggleMarkings(ent, (effect, markingStatus), false);
            markingStatus.CachedMarkings.Clear();
            PredictedQueueDel(effect);
        }
    }

    [SubscribeLocalEvent]
    private void OnApply(Entity<ModifyMarkingStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ToggleMarkings(args.Target, ent, true);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<ModifyMarkingStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        ToggleMarkings(args.Target, ent, false);
    }

    private void ToggleMarkings(EntityUid uid, Entity<ModifyMarkingStatusEffectComponent> status, bool apply)
    {
        if (!apply && status.Comp.CachedMarkings.Count == 0)
            return;

        if (!_visualBody.TryGatherMarkingsData(uid, [status.Comp.Layer], out _, out _, out var applied))
            return;

        if (!applied.TryGetValue(status.Comp.Organ, out var markingsSet))
            return;

        markingsSet = markingsSet.ShallowClone();

        foreach (var (layers, markings) in markingsSet)
        {
            markingsSet[layers] = markings.ShallowClone();
            var layerMarkings = markingsSet[layers];

            for (var i = 0; i < layerMarkings.Count; i++)
            {
                var currentMarking = layerMarkings[i];

                if (currentMarking.IsChildMarking)
                    continue;

                var currentMarkingId = currentMarking.MarkingId;

                string newMarkingId;

                if (apply)
                {
                    if (currentMarkingId.Id.EndsWith(status.Comp.Suffix))
                        continue;

                    newMarkingId = $"{currentMarkingId}{status.Comp.Suffix}";
                }
                else if (status.Comp.CachedMarkings.TryGetValue(currentMarkingId, out var marking))
                    newMarkingId = marking;
                else
                    newMarkingId = currentMarkingId;

                if (!ProtoMan.HasIndex<MarkingPrototype>(newMarkingId))
                    continue;

                if (apply)
                    status.Comp.CachedMarkings[newMarkingId] = currentMarkingId;

                layerMarkings[i] = new Marking(newMarkingId, layerMarkings[i].MarkingColors);
            }
        }

        if (apply)
            Dirty(status);

        _visualBody.ApplyMarkings(uid,
            new()
            {
                [status.Comp.Organ] = markingsSet
            });
    }
}
