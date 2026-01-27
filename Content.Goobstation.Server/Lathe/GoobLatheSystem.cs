using Content.Goobstation.Shared.Lathe;
using Content.Server.Lathe;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

public sealed class GoobLatheSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly LatheSystem _lathe = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private Dictionary<string, int> _totalMaterials = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, LatheQueueResetMessage>(OnLatheQueueResetMessage);
    }

    private void OnLatheQueueResetMessage(Entity<LatheComponent> ent, ref LatheQueueResetMessage args)
    {
        var (uid, comp) = ent;
        if (comp.Queue.Count == 0)
            return;

        _totalMaterials.Clear();

        // refund remaining items in the batch
        // there is no test to make sure this doesn't give infinite mats... goida
        foreach (var batch in comp.Queue)
        {
            var recipe = _proto.Index(batch.Recipe);
            var count = batch.ItemsRequested - batch.ItemsPrinted;
            foreach (var (mat, amount) in recipe.Materials)
            {
                var add = amount * count;
                if (recipe.ApplyMaterialDiscount)
                    add = (int) (add * comp.MaterialUseMultiplier);
                if (!_totalMaterials.ContainsKey(mat))
                    _totalMaterials[mat] = 0;
                _totalMaterials[mat] += add;
            }
        }

        if (!_materialStorage.CanChangeMaterialAmount(uid, _totalMaterials))
        {
            _popup.PopupEntity(Loc.GetString("lathe-queue-reset-material-overflow"), uid, args.Actor);
            return;
        }

        foreach (var (mat, amount) in _totalMaterials)
        {
            _materialStorage.TryChangeMaterialAmount(uid, mat, amount);
        }
        comp.Queue.Clear();
        _lathe.UpdateUserInterfaceState(uid, comp);
    }
}
