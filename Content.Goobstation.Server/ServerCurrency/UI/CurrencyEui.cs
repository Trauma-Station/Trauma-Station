// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.ServerCurrency;
using Content.Goobstation.Shared.ServerCurrency;
using Content.Goobstation.Shared.ServerCurrency.UI;
using Content.Server.Administration.Notes;
using Content.Server.EUI;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.ServerCurrency.UI;

public sealed class CurrencyEui : BaseEui
{
    [Dependency] private readonly ICommonCurrencyManager _currencyMan = default!;
    [Dependency] private readonly IAdminNotesManager _notesMan = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    public CurrencyEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new CurrencyEuiState();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (msg is not CurrencyEuiMsg.Buy buy)
            return;

        BuyToken(buy.TokenId, Player);
        StateDirty();
    }

    private async void BuyToken(ProtoId<TokenListingPrototype> tokenId, ICommonSession playerName)
    {
        var balance = _currencyMan.GetBalance(Player.UserId);

        if (!_protoMan.TryIndex<TokenListingPrototype>(tokenId, out var token))
            return;

        if (balance < token.Price)
            return;

        _currencyMan.RemoveCurrency(Player.UserId, token.Price);
        await _notesMan.AddAdminRemark(Player, Player.UserId, 0,
            Loc.GetString(token.AdminNote), 0, false, null);
    }
}
