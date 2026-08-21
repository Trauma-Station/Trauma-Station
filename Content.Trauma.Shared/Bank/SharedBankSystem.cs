using System.Linq;
using Content.Shared.Coordinates;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Stacks;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Trauma.Common.Bank;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Bank;

public abstract partial class SharedBankSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedObjectivesSystem _objectives = default!;


    private static readonly EntProtoId BankAccountObjectiveProto = "ObjectiveBankAccount";

    private List<NetEntity> _banks = new();
    private List<(NetEntity, ProtoId<CurrencyPrototype>, FixedPoint2)> _accountQueue = new();
    private TimeSpan _accountCheck = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<BankComponent, ComponentInit>(OnBankInit);
        SubscribeLocalEvent<BankComponent, DestructionEventArgs>(OnBankDestroyed);
    }

    public virtual void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId is not { } job || !_proto.TryIndex<JobPrototype>(job, out var jobProto))
            return;

        var moneyMarket = (GetNetEntity(args.Mob), jobProto.StartingCurrencyType, jobProto.StartingCurrency);
        if (GetAllBanks().Count == 0)
        {
            _accountQueue.Add(moneyMarket);
            return;
        }

        AddStartingAccount(args.Mob, jobProto.StartingCurrencyType, jobProto.StartingCurrency);
    }

    private void OnBankInit(Entity<BankComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BankId = $"{GetNetEntity(ent)}";
    }

    private void OnBankDestroyed(Entity<BankComponent> ent, ref DestructionEventArgs args)
    {
        var currency = _proto.Index(ent.Comp.Currency);
        foreach (var (_, money) in ent.Comp.Accounts)
        {
            PrintMoney(ent, money, currency);
        }
        ent.Comp.Accounts.Clear();
        ent.Comp.Passwords.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _accountCheck)
            return;

        _accountCheck = _timing.CurTime + TimeSpan.FromSeconds(1);

        if (GetAllBanks().FirstOrNull() is not { } bank || !HasComp<BankComponent>(GetEntity(bank)))
            return;

        foreach (var account in _accountQueue.ToList())
        {
            if (AddStartingAccount(account))
                _accountQueue.Remove(account);
        }
    }

    public List<NetEntity> GetAllBanks()
    {
        _banks.Clear();

        var query = EntityQueryEnumerator<BankComponent>();
        while (query.MoveNext(out var id, out _))
        {
            _banks.Add(GetNetEntity(id));
        }
        return _banks;
    }

    public FixedPoint2 GetMoneyInAccount(Entity<BankComponent?>? uid, string account)
    {
        BankComponent? comp = null;
        if (uid is not { } ent || !Resolve(ent, ref comp))
            return 0;

        return comp.Accounts.GetValueOrDefault(account, 0);
    }

    public FixedPoint2 AddCreditsAccountIIC(Entity<BankComponent> ent, string account, FixedPoint2 amount)
    {
        var currentMoney = ent.Comp.Accounts.GetValueOrDefault(account, 0);
        ent.Comp.Accounts[account] = currentMoney + amount;
        Dirty(ent);
        return currentMoney + amount;
    }

    /// <summary>
    /// Method for money into terminals
    /// </summary>
    public void InsertMoney(Entity<BankTerminalComponent> terminal, EntityUid user, Entity<CurrencyComponent, StackComponent> creditItem)
    {
        if (!Exists(creditItem))
            return;

        if (terminal.Comp.LinkedBank is not { } bank || !TryComp<BankComponent>(bank, out var bankComp) || !creditItem.Comp1.Price.TryGetValue(bankComp.Currency, out var unitPrice))
            return;

        _stack.ReduceCount((creditItem, creditItem.Comp2), (AddCreditsAccountIIC((bank, bankComp), terminal.Comp.LinkedAccount, creditItem.Comp2.Count * unitPrice) / unitPrice).Int());
    }

    /// <summary>
    /// Method for money into vending machines
    /// </summary>
    public void InsertMoney(Entity<MoneyStorageComponent> storage, Entity<CurrencyComponent, StackComponent> creditItem)
    {
        if (!Exists(creditItem))
            return;

        if (!creditItem.Comp1.Price.TryGetValue(storage.Comp.Currency, out var unitPrice))
            return;

        storage.Comp.MoneyBuffer += creditItem.Comp2.Count * unitPrice;
        Dirty(storage);
        _stack.ReduceCount((creditItem, creditItem.Comp2), creditItem.Comp2.Count);
    }

    /// <summary>
    /// Print money from something into thin air on an entity.
    /// </summary>
    public FixedPoint2 PrintMoney(EntityUid uid, FixedPoint2 amount, CurrencyPrototype currency, bool popup = false)
    {
        if (amount <= 0 || !Exists(uid))
            return amount;

        if (currency.Cash is not { } cash)
            return amount;

        var amountRemaining = amount;
        var coordinates = uid.ToCoordinates();
        var sortedCashValues = cash.Keys.OrderByDescending(x => x).ToList();
        EntityUid? money = null;
        foreach (var value in sortedCashValues)
        {
            var cashId = cash[value];
            var amountToSpawn = (int) MathF.Floor((float) (amountRemaining / value));
            for (var i = 0; i < amountToSpawn; i++)
            {
                var spawned = PredictedSpawnAtPosition(cashId, coordinates);
                if (money is not { } existingMoney)
                    money = spawned;
                else
                    _stack.TryMergeStacks(spawned, existingMoney, out _);
            }
            amountRemaining -= value * amountToSpawn;
        }
        if (money is { } ent && popup)
            _popup.PopupPredictedCoordinates($"Printed {amount} {Name(ent)}.", coordinates, uid, PopupType.Medium);
        return amountRemaining;
    }


    public FixedPoint2 TransferCreditAccountsIIC(Entity<BankComponent> ent, string moneyAccount, string transferAccount, FixedPoint2 amount)
    {
        var money = ent.Comp.Accounts.GetValueOrDefault(moneyAccount, 0);
        var moneyToTransfer = FixedPoint2.Max(money - amount, 0);
        ent.Comp.Accounts[moneyAccount] = money - moneyToTransfer;
        ent.Comp.Accounts[transferAccount] = ent.Comp.Accounts.GetValueOrDefault(transferAccount, 0) + moneyToTransfer;
        Dirty(ent);
        return moneyToTransfer;
    }

    /// <summary>
    /// Can account be accessed from this bank?
    /// </summary>
    public Entity<BankComponent>? CanAccessAccount(EntityUid bank, string account, string password)
    {
        if (!TryComp<BankComponent>(bank, out var bankComp))
        {
            _popup.PopupEntity("Access Failed: Bank is invalid.", bank);
            return null;
        }

        if (!bankComp.Accounts.ContainsKey(account))
        {
            _popup.PopupEntity($"Access Failed: {account} does not exist.", bank);
            return null;
        }

        if (!IsPasswordValid(account, password, (bank, bankComp)))
        {
            _popup.PopupEntity("Access Failed: Incorrect Password", bank);
            return null;
        }

        return (bank, bankComp);
    }

    /// <summary>
    /// Generate a random seven digit ID.
    /// </summary>
    public (string account, string password) GenerateSevenId(NetEntity seed)
    {
        var random = SharedRandomExtensions.PredictedRandom(_timing, seed);
        var account = "";
        var password = "";
        for (var i = 0; i < 7; i++)
        {
            account += random.Next(10).ToString();
        }
        for (var i = 0; i < 7; i++)
        {
            password += random.Next(10).ToString();
        }
        return (account, password);
    }

    public bool AddStartingAccount((NetEntity, ProtoId<CurrencyPrototype>, FixedPoint2) moneyMarket) => AddStartingAccount(GetEntity(moneyMarket.Item1), moneyMarket.Item2, moneyMarket.Item3);

    /// <summary>
    /// Add a starting account.
    /// </summary>
    public bool AddStartingAccount(EntityUid player, ProtoId<CurrencyPrototype> currency, FixedPoint2 amount)
    {
        Entity<BankComponent>? possibleBank = null;
        foreach (var banks in GetAllBanks())
        {
            var ebank = GetEntity(banks);
            if (!TryComp<BankComponent>(ebank, out var bankComp) || bankComp.Currency != currency)
                continue;

            possibleBank = (ebank, bankComp);
            break;
        }

        if (possibleBank is not { } bank)
            return false;

        var (account, password) = GenerateSevenId(GetNetEntity(bank));

        AddAccount(player, bank, account, password);
        bank.Comp.Accounts[account] = amount;
        Dirty(bank);

        return true;
    }

    public void AddAccount(EntityUid player, Entity<BankComponent> bank, string account, string password)
    {
        bank.Comp.Passwords[account] = password;
        Dirty(bank);

        AddAccountToMind(player, bank, account, password);
    }

    private void AddAccountToMind(EntityUid player, Entity<BankComponent> bank, string account, string password)
    {
        if (!_mind.TryGetMind(player, out var mind, out var mindComp))
            return;

        var mindEnt = (mind, mindComp);

        EntityUid? objective = null;
        if (!_mind.TryFindObjective(mindEnt, BankAccountObjectiveProto, out objective))
        {
            objective = Spawn(BankAccountObjectiveProto);
            _mind.AddObjective(mind, mindComp, objective.Value);
        }

        if (objective is not { } bankUid)
            return;

        if (!TryComp<BankAccountObjectiveComponent>(bankUid, out var bankComp))
        {
            PredictedQueueDel(bankUid);
            return;
        }

        bankComp.Details.Add((bank, account, password));

        Dirty(bankUid, bankComp);

        _objectives.GetProgress(bankUid, mindEnt);
    }

    public bool IsPasswordValid(string account, string password, Entity<BankComponent> bank)
        => bank.Comp.Passwords.TryGetValue(account, out var storedPassword) && storedPassword == password;
}
