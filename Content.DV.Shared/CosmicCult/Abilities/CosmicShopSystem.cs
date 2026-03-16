using Content.DV.Shared.CosmicCult.Components;

namespace Content.DV.Shared.CosmicCult.Abilities;

public sealed class CosmicShopSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicShop>(OnCosmicShop);
    }

    private void OnCosmicShop(Entity<CosmicCultComponent> ent, ref EventCosmicShop args)
    {
        _ui.TryToggleUi(args.Action.Owner, CosmicShopKey.Key, ent);
    }
}
