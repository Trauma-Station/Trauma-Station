using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.Bank;

[Serializable, NetSerializable]
public enum BankTerminalUiKey : byte
{
    Key
}

/// <summary>
/// Fired from server to client to send available banks.
/// </summary>
[Serializable, NetSerializable]
public sealed class PaykeyInterfaceState(List<NetEntity> banks) : BoundUserInterfaceState
{
    public List<NetEntity> Banks = banks;
}

/// <summary>
/// Fired from server to client to send bank terminal info.
/// </summary>
[Serializable, NetSerializable]
public sealed class BankTerminalInterfaceState(string account, string password, FixedPoint2 money, NetEntity? bank) : BoundUserInterfaceState
{
    public string Account = account;
    public string Password = password;
    public FixedPoint2 Money = money;
    public NetEntity? Bank = bank;
}

/// <summary>
/// Fried from client to server to open an account with bank.
/// </summary>
[Serializable, NetSerializable]
public sealed class BankTerminalCreateAccountMessage(string account, string password) : BoundUserInterfaceMessage
{
    public string Account = account;
    public string Password = password;
}

/// <summary>
/// Fired from client to server to sign out.
/// </summary>
[Serializable, NetSerializable]
public sealed class BankTerminalSignInMessage(string account, string password) : BoundUserInterfaceMessage
{
    public string Account = account;
    public string Password = password;
}

/// <summary>
/// Fired from client to server to sign out.
/// </summary>
[Serializable, NetSerializable]
public sealed class BankTerminalSignOutMessage() : BoundUserInterfaceMessage;

/// <summary>
/// Fired from client to server to sign out.
/// </summary>
[Serializable, NetSerializable]
public sealed class BankTerminalSetBankMessage(NetEntity bank) : BoundUserInterfaceMessage
{
    public NetEntity Bank = bank;
}

/// <summary>
/// Fired from client to server to withdraw money.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BankTerminalWithdrawMoneyMessage(FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public FixedPoint2 Amount = amount;
}

/// <summary>
/// Fired from client to server to send money to another account.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BankTerminalSendMoneyMessage(string account, FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public string Account = account;
    public FixedPoint2 Amount = amount;
}


/// <summary>
/// Fired from the client to server to state what amoune of money to change to an ICC account.
/// </summary>
[Serializable, NetSerializable]
public sealed class BankSendToIICMessage(string moneyAccount, string password, string transferAccount, NetEntity bank, int amount) : BoundUserInterfaceMessage
{
    public string MoneyAccount = moneyAccount;
    public string Password = password;
    public string TransferAccount = transferAccount;
    public NetEntity Bank = bank;
    public int Amount = amount;
}

/// <summary>
/// Fired from the client to server to state what amoune of money to change to an OOC account.
/// </summary>
[Serializable, NetSerializable]
public sealed class BankSendToOOCMessage(string account, string password, NetEntity bank, int amount) : BoundUserInterfaceMessage
{
    public string Account = account;
    public string Password = password;
    public NetEntity Bank = bank;
    public int Amount = amount;
}
