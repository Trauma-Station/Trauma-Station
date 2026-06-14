using Content.Goobstation.Common.ServerCurrency;
using Content.Trauma.Common.LinkAccount;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby;

public sealed partial class LobbyState
{
    [Dependency] private ICommonCurrencyManager _currency = default!;
    [Dependency] private ILinkAccountManager _linkAccount = default!;

    public static Action<LobbyState>? OnCreated;
    public Action? OnTogglePatronPerksWindow; // TODO: make this use ui injection with OnCreated ^

    private void StartupTrauma()
    {
        Lobby!.CharacterPreview.PatronPerks.OnPressed += OnPatronPerksPressed;
        _currency.ClientBalanceChange += UpdatePlayerBalance;
        OnCreated?.Invoke(this);
    }

    private void ShutdownTrauma()
    {
        Lobby!.CharacterPreview.PatronPerks.OnPressed -= OnPatronPerksPressed;
        _currency.ClientBalanceChange -= UpdatePlayerBalance;
    }

    private void UpdateLobbyUiTrauma()
    {
        Lobby!.CharacterPreview.PatronPerks.Visible = _linkAccount.CanViewPatronPerks();
        UpdatePlayerBalance();
    }

    private void OnPatronPerksPressed(BaseButton.ButtonEventArgs obj)
    {
        OnTogglePatronPerksWindow?.Invoke();
    }

    private void UpdatePlayerBalance()
    {
        if (Lobby is not { } lobby)
            return;

        lobby.Balance.Text = _currency.Stringify(_currency.GetBalance());
    }
}
