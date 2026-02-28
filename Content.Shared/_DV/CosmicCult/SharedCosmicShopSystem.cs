using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.CosmicCult;

public partial class SharedCosmicShopSystem : EntitySystem
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
    }

    private void OnUIOpened(Entity<CosmicShopComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!TryComp<CosmicCultComponent>(args.Actor, out var cultComp))
            return;

        _ui.SetUiState(ent.Owner, CosmicShopKey.Key, new CosmicShopBuiState());
    }

    #region UI listeners
    private void OnInfluenceSelected(Entity<CosmicShopComponent> ent, ref InfluenceSelectedMessage args)
    {
        if (!_prototype.TryIndex(args.InfluenceProtoId, out var proto) || !TryComp<CosmicCultComponent>(args.Actor, out var cultComp))
            return;

        if (cultComp.EntropyBudget < proto.Cost || cultComp.OwnedInfluences.Contains(proto) || args.Actor == null)
            return;

        _audio.PlayLocal(ent.Comp.PurchaseSfx, args.Actor, args.Actor);
        cultComp.OwnedInfluences.Add(proto);

        if (proto.InfluenceType == "influence-type-active")
        {
            var actionEnt = _actions.AddAction(args.Actor, proto.Action);
            cultComp.ActionEntities.Add(actionEnt);
        }
        else if (proto.InfluenceType == "influence-type-passive")
        {
            UnlockPassive(args.Actor, proto); //Not unlocking an action? call the helper function to add the influence's passive effects
        }

        cultComp.EntropyBudget -= proto.Cost;
        Dirty(args.Actor, cultComp); //force an update to make sure that the client has the correct set of owned abilities

        _ui.SetUiState(ent.Owner, CosmicShopKey.Key, new CosmicShopBuiState());
    }
    #endregion

    private void UnlockPassive(EntityUid cultist, InfluencePrototype proto)
    {
        if (proto.Add != null)
            _entMan.AddComponents(cultist, proto.Add);

        if (proto.Remove != null)
            _entMan.RemoveComponents(cultist, proto.Remove);
    }
}
