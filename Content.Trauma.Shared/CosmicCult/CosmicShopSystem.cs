// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;
using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.CosmicCult;

public sealed partial class CosmicShopSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityQuery<CosmicCultComponent> _cultistQuery = default!;

    [SubscribeLocalEvent]
    private void OnInfluenceSelected(Entity<CosmicShopComponent> ent, ref InfluenceSelectedMessage args)
    {
        var user = args.Actor;
        if (!ProtoMan.TryIndex(args.InfluenceProtoId, out var proto) || !_cultistQuery.TryComp(user, out var cultist))
            return;

        if (cultist.EntropyBudget < proto.Cost || cultist.OwnedInfluences.Contains(proto))
            return;

        cultist.EntropyBudget -= proto.Cost;
        DirtyField(user, cultist, nameof(CosmicCultComponent.EntropyBudget));
        cultist.OwnedInfluences.Add(proto);
        DirtyField(user, cultist, nameof(CosmicCultComponent.OwnedInfluences));

        _audio.PlayLocal(ent.Comp.PurchaseSfx, user, user);

        if (!proto.Passive)
        {
            if (_actions.AddAction(user, proto.Action) is not { } action)
                return;

            cultist.BoughtActions.Add(action);
            DirtyField(user, cultist, nameof(CosmicCultComponent.BoughtActions));
        }
        else
        {
            if (proto.Add != null)
                EntityManager.AddComponents(user, proto.Add);

            if (proto.Remove != null)
                EntityManager.RemoveComponents(user, proto.Remove);
        }
    }

    [SubscribeLocalEvent]
    private void OnRespecConfirmed(Entity<CosmicShopComponent> ent, ref RespecConfirmedMessage args)
    {
        var user = args.Actor;
        if (!_cultistQuery.TryComp(user, out var cultist) || cultist.RespecsAvailable <= 0)
            return;

        if (cultist.OwnedInfluences.Count == 0)
            return; // Nothing to respec

        cultist.RespecsAvailable--;

        foreach (var influence in cultist.OwnedInfluences)
        {
            if (!ProtoMan.Resolve(influence, out var proto))
                continue;

            cultist.OwnedInfluences.Remove(influence);
            cultist.UnlockedInfluences.Add(influence);
            cultist.EntropyBudget += proto.Cost;

            if (proto.Passive)
            {
                if (proto.Add != null)
                    EntityManager.RemoveComponents(user, proto.Add);

                if (proto.Remove != null)
                    EntityManager.AddComponents(user, proto.Remove); // This will probably not work well, but there are currently no influences that remove components. Should be careful with those in the future.
            }
        }

        DirtyFields(user, cultist, null,
            nameof(CosmicCultComponent.RespecsAvailable),
            nameof(CosmicCultComponent.OwnedInfluences),
            nameof(CosmicCultComponent.UnlockedInfluences),
            nameof(CosmicCultComponent.EntropyBudget));

        if (cultist.BoughtActions.Count > 0)
        {
            foreach (var action in cultist.BoughtActions)
            {
                _actions.RemoveAction(action);
            }
            cultist.BoughtActions.Clear();
            DirtyField(user, cultist, nameof(CosmicCultComponent.BoughtActions));
        }

        _audio.PlayLocal(ent.Comp.PurchaseSfx, user, user);
    }
}
