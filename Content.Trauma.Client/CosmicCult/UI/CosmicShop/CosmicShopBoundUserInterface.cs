// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;

namespace Content.Trauma.Client.CosmicCult.UI.CosmicShop;

public sealed partial class CosmicShopBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private CosmicShopMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CosmicShopMenu>();

        _menu.OnGainButtonPressed += id => SendPredictedMessage(new InfluenceSelectedMessage(id));
        _menu.OnLevelUpConfirmed += () => SendPredictedMessage(new LevelUpconfirmedMessage());
        _menu.OnRespecConfirmed += () => SendPredictedMessage(new RespecConfirmedMessage());
    }
}
