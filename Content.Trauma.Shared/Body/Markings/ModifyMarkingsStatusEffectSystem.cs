// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.StatusEffectNew;

namespace Content.Trauma.Shared.Body.Markings;

public sealed partial class ModifyMarkingsStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

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
                else
                {
                    if (currentMarkingId.Id.EndsWith(status.Comp.Suffix))
                    {
                        newMarkingId = currentMarkingId.Id[..^status.Comp.Suffix.Length];
                    }
                    else
                    {
                        newMarkingId = currentMarkingId;
                        Log.Warning($"Unable to revert marking override for {currentMarkingId}");
                    }
                }

                if (!ProtoMan.HasIndex<MarkingPrototype>(newMarkingId))
                {
                    Log.Warning($"{ToPrettyString(uid):ent} tried toggling marking {newMarkingId} that doesn't exist");
                    continue;
                }

                layerMarkings[i] = new Marking(newMarkingId, layerMarkings[i].MarkingColors);
            }
        }

        _visualBody.ApplyMarkings(uid,
            new()
            {
                [status.Comp.Organ] = markingsSet
            });
    }
}
