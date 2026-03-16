using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._DV.CosmicCult.UI.CosmicShop;

public sealed class CosmicShopBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private CosmicShopMenu? _menu;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CosmicShopMenu>();

        _menu.OnGainButtonPressed += OnInfluenceSelected;
        _menu.OnLevelUpConfirmed += OnLevelUpConfirmed;
        _menu.OnRespecConfirmed += OnRespecConfirmed;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CosmicShopBuiState buiState
        || !_entMan.TryGetComponent<CosmicCultComponent>(_player.LocalEntity, out var comp))
            return;

        _menu?.UpdateState(comp);
    }

    private void OnInfluenceSelected(ProtoId<InfluencePrototype> selectedInfluence) =>
        SendPredictedMessage(new InfluenceSelectedMessage(selectedInfluence));

    private void OnLevelUpConfirmed() =>
        SendPredictedMessage(new LevelUpconfirmedMessage());

    private void OnRespecConfirmed() =>
        SendPredictedMessage(new RespecConfirmedMessage());
}
