using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.CosmicCult;

public sealed partial class CosmicShopSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CosmicShopComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<CosmicShopComponent, InfluenceSelectedMessage>(OnInfluenceSelected);
        SubscribeLocalEvent<CosmicShopComponent, RespecConfirmedMessage>(OnRespecConfirmed);
    }

    private void OnUIOpened(Entity<CosmicShopComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!HasComp<CosmicCultComponent>(args.Actor))
            return;

        _ui.SetUiState(ent.Owner, CosmicShopKey.Key, new CosmicShopBuiState());
    }

    #region UI listeners
    private void OnInfluenceSelected(Entity<CosmicShopComponent> ent, ref InfluenceSelectedMessage args)
    {
        var user = args.Actor;
        if (!_prototype.TryIndex(args.InfluenceProtoId, out var proto) || !TryComp<CosmicCultComponent>(user, out var cultComp))
            return;

        if (cultComp.EntropyBudget < proto.Cost || cultComp.OwnedInfluences.Contains(proto))
            return;

        _audio.PlayLocal(ent.Comp.PurchaseSfx, user, user);
        cultComp.OwnedInfluences.Add(proto);

        if (!proto.Passive)
        {
            var actionEnt = _actions.AddAction(user, proto.Action);
            cultComp.ActionEntities.Add(actionEnt);
        }
        else
        {
            if (proto.Add != null)
                _entMan.AddComponents(args.Actor, proto.Add);

            if (proto.Remove != null)
                _entMan.RemoveComponents(args.Actor, proto.Remove);
        }

        cultComp.EntropyBudget -= proto.Cost;
        Dirty(user, cultComp); //force an update to make sure that the client has the correct set of owned abilities

        _ui.SetUiState(ent.Owner, CosmicShopKey.Key, new CosmicShopBuiState());
    }

    private void OnRespecConfirmed(Entity<CosmicShopComponent> ent, ref RespecConfirmedMessage args)
    {
        if (!TryComp<CosmicCultComponent>(args.Actor, out var cultComp) || cultComp.RespecsAvailable <= 0)
            return;

        if (cultComp.OwnedInfluences.Count == 0)
            return; // Nothing to respec

        foreach (var influence in cultComp.OwnedInfluences)
        {
            if (!_prototype.Resolve(influence, out var proto)) continue;
            cultComp.OwnedInfluences.Remove(influence);
            cultComp.UnlockedInfluences.Add(influence);
            cultComp.EntropyBudget += proto.Cost;

            if (proto.Passive)
            {
                if (proto.Add != null)
                    _entMan.RemoveComponents(args.Actor, proto.Add);

                if (proto.Remove != null)
                    _entMan.AddComponents(args.Actor, proto.Remove); // This will probably not work well, but there are currently no influences that remove components. Should be careful with those in the future.
            }
        }
        foreach (var action in cultComp.ActionEntities)
            _actions.RemoveAction(action);
        cultComp.ActionEntities.Clear();

        _audio.PlayLocal(ent.Comp.PurchaseSfx, args.Actor, args.Actor);
        cultComp.RespecsAvailable--;
        Dirty(args.Actor, cultComp); //force an update to make sure that the client has the correct set of owned abilities

        _ui.SetUiState(ent.Owner, CosmicShopKey.Key, new CosmicShopBuiState());
    }
    #endregion
}
