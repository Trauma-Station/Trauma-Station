// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.ServerCurrency;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Goobstation.Server.ServerCurrency.Commands;

[AnyCommand]
public sealed partial class BalanceServerCurrencyCommand : IConsoleCommand
{
    [Dependency] private ICommonCurrencyManager _currency = default!;
    [Dependency] private IChatManager _chat = default!;

    public string Command => Loc.GetString("server-currency-balance-command");
    public string Description => Loc.GetString("server-currency-balance-command-description");
    public string Help => Loc.GetString("server-currency-balance-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if(shell.Player is not { } player){
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var balance = Loc.GetString("server-currency-balance-command-return",
            ("balance", _currency.Stringify(_currency.GetBalance(shell.Player.UserId))));

        _chat.ChatMessageToOne(ChatChannel.Local, balance, balance, EntityUid.Invalid, false, shell.Player.Channel);
        shell.WriteLine(balance);
    }
}

[AnyCommand]
public sealed partial class GiftServerCurrencyCommand : IConsoleCommand
{
    [Dependency] private ICommonCurrencyManager _currency = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerManager _player = default!;

    public string Command => Loc.GetString("server-currency-gift-command");
    public string Description => Loc.GetString("server-currency-gift-command-description");
    public string Help => Loc.GetString("server-currency-gift-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (!_player.TryGetUserId(args[0], out var targetPlayer))
        {
            shell.WriteError(Loc.GetString("server-currency-command-error-1"));
            return;
        }
        else if (targetPlayer == shell.Player.UserId)
        {
            shell.WriteError(Loc.GetString("server-currency-gift-command-error-1"));
            return;
        }

        if (!int.TryParse(args[1], out int amount))
        {
            shell.WriteError(Loc.GetString("server-currency-command-error-2"));
            return;
        }

        amount = Math.Abs(amount);

        if (amount == 0)
            amount = 1; // Trolled

        if (!_currency.CanAfford(shell.Player.UserId, amount, out int balance))
        {
            shell.WriteError(Loc.GetString("server-currency-gift-command-error-2", ("balance", balance)));
            return;
        }

        _currency.TransferCurrency(shell.Player.UserId, targetPlayer, amount);

        var giver = Loc.GetString("server-currency-gift-command-giver", ("player", args[0]), ("amount", _currency.Stringify(amount)));
        var reciever = Loc.GetString("server-currency-gift-command-reciever", ("player", shell.Player.Name), ("amount", _currency.Stringify(amount)));

        if (_player.TryGetSessionById(targetPlayer, out var targetPlayerSession))
            _chat.ChatMessageToOne(ChatChannel.Local, reciever, reciever, EntityUid.Invalid, false, targetPlayerSession.Channel);
        _chat.ChatMessageToOne(ChatChannel.Local, giver, giver, EntityUid.Invalid, false, shell.Player.Channel);

        shell.WriteLine(giver);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
            2 => CompletionResult.FromHint(Loc.GetString("server-currency-command-completion-2")),
            _ => CompletionResult.Empty
        };
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed partial class AddServerCurrencyCommand : IConsoleCommand
{
    [Dependency] private ICommonCurrencyManager _currency = default!;
    [Dependency] private IPlayerManager _player = default!;

    public string Command => Loc.GetString("server-currency-add-command");
    public string Description => Loc.GetString("server-currency-add-command-description");
    public string Help => Loc.GetString("server-currency-add-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_player.TryGetUserId(args[0], out var targetPlayer))
        {
            shell.WriteError(Loc.GetString("server-currency-command-error-1"));
            return;
        }

        if (!int.TryParse(args[1], out int currency))
        {
            shell.WriteError(Loc.GetString("server-currency-command-error-2"));
            return;
        }

        var newCurrency = _currency.Stringify(_currency.AddCurrency(targetPlayer, currency));
        shell.WriteLine(Loc.GetString("server-currency-command-return", ("player", args[0]), ("balance", newCurrency)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
            2 => CompletionResult.FromHint(Loc.GetString("server-currency-command-completion-2")),
            _ => CompletionResult.Empty
        };
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed partial class RemoveServerCurrencyCommand : IConsoleCommand
{
    [Dependency] private ICommonCurrencyManager _currency = default!;
    [Dependency] private IPlayerManager _player = default!;

    public string Command => Loc.GetString("server-currency-remove-command");
    public string Description => Loc.GetString("server-currency-remove-command-description");
    public string Help => Loc.GetString("server-currency-remove-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_player.TryGetUserId(args[0], out var targetPlayer))
        {
            shell.WriteError(Loc.GetString("server-currency-command-error-1"));
            return;
        }

        if (!int.TryParse(args[1], out int currency))
        {
            shell.WriteError(Loc.GetString("server-currency-command-error-2"));
            return;
        }

        var newCurrency = _currency.Stringify(_currency.RemoveCurrency(targetPlayer, currency));
        shell.WriteLine(Loc.GetString("server-currency-command-return", ("player", args[0]), ("balance", newCurrency)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
            2 => CompletionResult.FromHint(Loc.GetString("server-currency-command-completion-2")),
            _ => CompletionResult.Empty
        };
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed partial class SetServerCurrencyCommand : IConsoleCommand
{
    [Dependency] private ICommonCurrencyManager _currency = default!;
    [Dependency] private IPlayerManager _player = default!;

    public string Command => Loc.GetString("server-currency-set-command");
    public string Description => Loc.GetString("server-currency-set-command-description");
    public string Help => Loc.GetString("server-currency-set-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_player.TryGetUserId(args[0], out var targetPlayer))
        {
            shell.WriteError(Loc.GetString("server-currency-command-error-1"));
            return;
        }

        if (!int.TryParse(args[1], out int currency))
        {
            shell.WriteError(Loc.GetString("server-currency-command-error-2"));
            return;
        }

        _currency.SetBalance(targetPlayer, currency);
        var newCurrency = _currency.Stringify(currency);
        shell.WriteLine(Loc.GetString("server-currency-command-return", ("player", args[0]), ("balance", newCurrency)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
            2 => CompletionResult.FromHint(Loc.GetString("server-currency-command-completion-2")),
            _ => CompletionResult.Empty
        };
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed partial class GetServerCurrencyCommand : IConsoleCommand
{
    [Dependency] private ICommonCurrencyManager _currency = default!;
    [Dependency] private IPlayerManager _player = default!;

    public string Command => Loc.GetString("server-currency-get-command");
    public string Description => Loc.GetString("server-currency-get-command-description");
    public string Help => Loc.GetString("server-currency-get-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_player.TryGetUserId(args[0], out var targetPlayer))
        {
            shell.WriteError(Loc.GetString("server-currency-command-error-1"));
            return;
        }

        var currency = _currency.Stringify(_currency.GetBalance(targetPlayer));
        shell.WriteLine(Loc.GetString("server-currency-command-return", ("player", args[0]), ("balance", currency)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("server-currency-command-completion-1")),
            _ => CompletionResult.Empty
        };
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed partial class WipeServerCurrencyCommand : IConsoleCommand
{
    [Dependency] private ICommonCurrencyManager _currency = default!;

    public string Command => "wipecurrency";
    public string Description => "Wipe the entire server's currency database...";
    public string Help => "wipecurrency [codephrase]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 || args[0] != "La-li-lu-le-lo")
        {
            shell.WriteError("EVA, say the password.");
            return;
        }

        await _currency.Wipe();
        shell.WriteLine("You're face, to face, with the man who sold the world");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("Who are the patriots?"),
            _ => CompletionResult.Empty
        };
    }
}
