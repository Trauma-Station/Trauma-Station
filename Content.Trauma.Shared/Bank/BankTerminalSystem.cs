using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Store.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Bank;

public sealed partial class BankTerminalSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBankSystem _bank = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BankTerminalComponent, BeforeActivatableUIOpenEvent>(OnOpen);
        SubscribeLocalEvent<BankTerminalComponent, InteractUsingEvent>(OnInsertMoney);
        SubscribeLocalEvent<BankTerminalComponent, BankTerminalSendMoneyMessage>(OnSendMoney);
        SubscribeLocalEvent<BankTerminalComponent, BankTerminalCreateAccountMessage>(OnCreateAccount);
        SubscribeLocalEvent<BankTerminalComponent, BankTerminalWithdrawMoneyMessage>(OnWithdrawMoney);
        SubscribeLocalEvent<BankTerminalComponent, BankTerminalSignInMessage>(OnSignIn);
        SubscribeLocalEvent<BankTerminalComponent, BankTerminalSignOutMessage>(OnSignOut);
        SubscribeLocalEvent<BankTerminalComponent, BankTerminalSetBankMessage>(OnSetBank);
    }

    private void OnOpen(Entity<BankTerminalComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new PaykeyInterfaceState(_bank.GetAllBanks()));
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new BankTerminalInterfaceState(ent.Comp.LinkedAccount, ent.Comp.LinkedPassword, _bank.GetMoneyInAccount(ent.Comp.LinkedBank, ent.Comp.LinkedAccount), GetNetEntity(ent.Comp.LinkedBank)));
    }

    private void OnInsertMoney(Entity<BankTerminalComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<CurrencyComponent>(args.Used, out var currency) || !TryComp<StackComponent>(args.Used, out var stack))
        {
            _popup.PopupPredicted($"{Name(args.Used)} is not a valid currency.", args.User, args.User, PopupType.Medium);
            return;
        }

        args.Handled = true;
        _bank.InsertMoney(ent, args.User, (args.Used, currency, stack));
        _audio.PlayPvs(ent.Comp.SoundOnInsertMoney, ent);
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new BankTerminalInterfaceState(ent.Comp.LinkedAccount, ent.Comp.LinkedPassword, _bank.GetMoneyInAccount(ent.Comp.LinkedBank, ent.Comp.LinkedAccount), GetNetEntity(ent.Comp.LinkedBank)));
    }

    private void OnSendMoney(Entity<BankTerminalComponent> ent, ref BankTerminalSendMoneyMessage args)
    {
        if (ent.Comp.LinkedBank is not { } bankUid || _bank.CanAccessAccount(bankUid, ent.Comp.LinkedAccount, ent.Comp.LinkedPassword) is not { } bank)
            return;

        var transferedAmount = _bank.TransferCreditAccountsIIC(bank, ent.Comp.LinkedAccount, args.Account, args.Amount);

        _audio.PlayPvs(ent.Comp.SoundOnTransfer, ent);
        if (transferedAmount == 0)
        {
            _popup.PopupEntity("Transaction Failed: Insufficient Funds Detected.", ent.Owner);
            return;
        }
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new BankTerminalInterfaceState(ent.Comp.LinkedAccount, ent.Comp.LinkedPassword, _bank.GetMoneyInAccount(ent.Comp.LinkedBank, ent.Comp.LinkedAccount), GetNetEntity(ent.Comp.LinkedBank)));
    }

    private void OnCreateAccount(Entity<BankTerminalComponent> ent, ref BankTerminalCreateAccountMessage args)
    {
        if (ent.Comp.LinkedBank is not { } bank || !TryComp<BankComponent>(bank, out var bankComp))
        {
            _popup.PopupEntity("Account Creation Failed: Bank Terminal Malfunction Detected - Bank is invalid.", ent.Owner);
            return;
        }

        if (bankComp.Accounts.ContainsKey(args.Account))
        {
            _popup.PopupEntity($"Account Creation Failed: Bank Terminal Malfunction Detected - {args.Account} already exists.", ent.Owner);
            return;
        }

        bankComp.Accounts[args.Account] = 0;
        bankComp.Passwords[args.Account] = args.Password;
        Dirty(bank, bankComp);
        _bank.AddAccount(args.Actor, (bank, bankComp), args.Account, args.Password);
        _audio.PlayPvs(ent.Comp.SoundOnCreateAccount, ent);
        ent.Comp.LinkedAccount = args.Account;
        ent.Comp.LinkedPassword = args.Password;
        Dirty(ent);
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new BankTerminalInterfaceState(ent.Comp.LinkedAccount, ent.Comp.LinkedPassword, _bank.GetMoneyInAccount(ent.Comp.LinkedBank, ent.Comp.LinkedAccount), GetNetEntity(ent.Comp.LinkedBank)));
    }

    private void OnWithdrawMoney(Entity<BankTerminalComponent> ent, ref BankTerminalWithdrawMoneyMessage args)
    {
        if (ent.Comp.LinkedBank is not { } bankUid || _bank.CanAccessAccount(bankUid, ent.Comp.LinkedAccount, ent.Comp.LinkedPassword) is not { } bank)
            return;

        var currentMoney = bank.Comp.Accounts[ent.Comp.LinkedAccount];
        if (currentMoney < args.Amount)
        {
            _popup.PopupEntity("Withdrawal Failed: Insufficient Funds Detected.", ent.Owner);
            return;
        }

        bank.Comp.Accounts[ent.Comp.LinkedAccount] = currentMoney - (args.Amount - _bank.PrintMoney(ent.Owner, args.Amount, _proto.Index(bank.Comp.Currency), true));
        Dirty(bank);
        _audio.PlayPvs(ent.Comp.SoundOnWithdrawMoney, ent);
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new BankTerminalInterfaceState(ent.Comp.LinkedAccount, ent.Comp.LinkedPassword, _bank.GetMoneyInAccount(ent.Comp.LinkedBank, ent.Comp.LinkedAccount), GetNetEntity(ent.Comp.LinkedBank)));
    }

    private void OnSignIn(Entity<BankTerminalComponent> ent, ref BankTerminalSignInMessage args)
    {
        if (ent.Comp.LinkedBank is not { } bankUid || _bank.CanAccessAccount(bankUid, args.Account, args.Password) is not { } bank)
            return;

        ent.Comp.LinkedAccount = args.Account;
        ent.Comp.LinkedPassword = args.Password;

        Dirty(ent);
        _audio.PlayPvs(ent.Comp.SoundOnSignIn, ent);
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new BankTerminalInterfaceState(ent.Comp.LinkedAccount, ent.Comp.LinkedPassword, _bank.GetMoneyInAccount(ent.Comp.LinkedBank, ent.Comp.LinkedAccount), GetNetEntity(ent.Comp.LinkedBank)));
    }

    private void OnSignOut(Entity<BankTerminalComponent> ent, ref BankTerminalSignOutMessage args)
    {
        ent.Comp.LinkedAccount = "";
        ent.Comp.LinkedPassword = "";
        Dirty(ent);
        _audio.PlayPvs(ent.Comp.SoundOnSignOut, ent);
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new BankTerminalInterfaceState(ent.Comp.LinkedAccount, ent.Comp.LinkedPassword, _bank.GetMoneyInAccount(ent.Comp.LinkedBank, ent.Comp.LinkedAccount), GetNetEntity(ent.Comp.LinkedBank)));
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new PaykeyInterfaceState(_bank.GetAllBanks()));
    }

    private void OnSetBank(Entity<BankTerminalComponent> ent, ref BankTerminalSetBankMessage args)
    {
        ent.Comp.LinkedBank = GetEntity(args.Bank);
        _ui.SetUiState(ent.Owner, BankTerminalUiKey.Key, new PaykeyInterfaceState(_bank.GetAllBanks()));
    }
}
