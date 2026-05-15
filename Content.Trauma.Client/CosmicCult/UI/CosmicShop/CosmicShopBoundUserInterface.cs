// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult;
using Content.Trauma.Shared.CosmicCult.Components;
using Content.Trauma.Shared.CosmicCult.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.Player;

namespace Content.Trauma.Client.CosmicCult.UI.CosmicShop;

public sealed partial class CosmicShopBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private CosmicShopMenu? _menu;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPlayerManager _player = default!;

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
