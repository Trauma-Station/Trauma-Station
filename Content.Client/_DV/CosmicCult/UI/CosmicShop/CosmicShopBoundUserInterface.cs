using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Prototypes;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._DV.CosmicCult.UI.CosmicShop;

public sealed class CosmicShopBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private CosmicShopMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CosmicShopMenu>();

        _menu.OnGainButtonPressed += OnInfluenceSelected;
        _menu.OnLevelUpConfirmed += OnLevelUpConfirmed;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CosmicShopBuiState buiState)
            return;

        _menu?.UpdateState(buiState);
    }

    private void OnInfluenceSelected(ProtoId<InfluencePrototype> selectedInfluence) =>
        SendPredictedMessage(new InfluenceSelectedMessage(selectedInfluence));

    private void OnLevelUpConfirmed() =>
        SendPredictedMessage(new LevelUpconfirmedMessage());
}
