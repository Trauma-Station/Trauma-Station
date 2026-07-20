// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.ServerCurrency;
using Content.Shared.Popups;
using Robust.Shared.Configuration;

namespace Content.Goobstation.Server.ServerCurrency;

/// <summary>
/// Connects <see cref="ServerCurrencyManager"/> to the simulation state.
/// </summary>
public sealed partial class ServerCurrencySystem : EntitySystem
{
    [Dependency] private ICommonCurrencyManager _currencyMan = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        _currencyMan.BalanceChange += OnBalanceChange;
        SubscribeNetworkEvent<PlayerBalanceRequestEvent>(OnBalanceRequest);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _currencyMan.BalanceChange -= OnBalanceChange;
    }

    private void OnBalanceRequest(PlayerBalanceRequestEvent ev, EntitySessionEventArgs eventArgs)
    {
        var senderSession = eventArgs.SenderSession;
        var balance = _currencyMan.GetBalance(senderSession.UserId);
        RaiseNetworkEvent(new PlayerBalanceUpdateEvent(balance, balance), senderSession);
    }

    /// <summary>
    /// Calls event that when a player's balance is updated.
    /// Also handles popups
    /// </summary>
    private void OnBalanceChange(PlayerBalanceChangeEvent ev)
    {
        RaiseNetworkEvent(new PlayerBalanceUpdateEvent(ev.NewBalance, ev.OldBalance), ev.UserSes);

        if (ev.UserSes.AttachedEntity is not {} user)
            return;

        if (ev.NewBalance > ev.OldBalance)
            _popup.PopupEntity("+" + _currencyMan.Stringify(ev.NewBalance - ev.OldBalance), user, user, PopupType.Medium);
        else if (ev.NewBalance < ev.OldBalance)
            _popup.PopupEntity("-" + _currencyMan.Stringify(ev.OldBalance - ev.NewBalance), user, user, PopupType.MediumCaution);
        // I really wanted to do some fancy shit where we also display a little sprite next to the pop-up, but that gets pretty complex for such a simple interaction, so, you get this.
    }
}
