// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Systems;
using Content.Trauma.Shared.Bank;

namespace Content.Trauma.Client.Bank.Ui;

public sealed partial class BankTerminalBoundUserInterface : BoundUserInterface
{
    private BankTerminalWindow? _window;
    private List<NetEntity> _cachedBanks = new();
    private readonly BankSystem _bank;

    public BankTerminalBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _bank = EntMan.System<BankSystem>();

    }

    protected override void Open()
    {
        base.Open();

        _window = new BankTerminalWindow();
        _window.OnClose += Close;

        _window.OnBankSelected += bankId =>
        {
            if (!_cachedBanks.TryGetValue(bankId, out var bank))
                return;

            SendMessage(new BankTerminalSetBankMessage(bank));
        };

        _window.OnSignInRequested += (account, pass) =>
            SendMessage(new BankTerminalSignInMessage(account, pass));

        _window.OnCreateAccountRequested += (account, pass) =>
            SendMessage(new BankTerminalCreateAccountMessage(account, pass));

        _window.OnSignOutRequested += () =>
            SendMessage(new BankTerminalSignOutMessage());

        _window.OnWithdrawRequested += amount =>
            SendMessage(new BankTerminalWithdrawMoneyMessage(amount));

        _window.OnSendMoneyRequested += (targetAccount, amount) =>
            SendMessage(new BankTerminalSendMoneyMessage(targetAccount, amount));

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PaykeyInterfaceState paykeyState)
        {
            _cachedBanks = paykeyState.Banks;
            _window?.UpdateAvailableBanks(_bank.GetBankIDs(_cachedBanks));
            return;
        }

        if (state is BankTerminalInterfaceState bankTerminalState && bankTerminalState.Bank is { } bank)
        {
            _window?.UpdateAccount(bankTerminalState.Account, bankTerminalState.Password, bankTerminalState.Money, bank, _bank.GetBankID(bank));
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        _window?.Close();
        //_window?.Dispose();
    }
}
