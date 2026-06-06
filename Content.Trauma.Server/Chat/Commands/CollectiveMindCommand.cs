// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Chat.Commands;

[AnyCommand]
public sealed class CollectiveMindCommand : IConsoleCommand
{
    public string Command => "cmsay";
    public string Description => "Send chat messages to the collective mind.";
    public string Help => "cmsay <text>";

    private ChatSystem? _chat;
    private MobStateSystem? _mob;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not ICommonSession player)
        {
            shell.WriteError("This command cannot be run from the server.");
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not { } mob)
        {
            shell.WriteError("You don't have an entity!");
            return;
        }

        var ent = IoCManager.Resolve<IEntityManager>();
        if (!ent.TryGetComponent<CollectiveMindComponent>(mob, out var mind))
        {
            shell.WriteError("You don't have CollectiveMind!");
            return;
        }

        // Skip dead/critical check if CanUseInCrit is enabled
        if (!mind.CanUseInCrit)
        {
            _mob ??= ent.System<MobStateSystem>();
            if (!_mob.IsAlive(mob))
            {
                shell.WriteError("You cannot use the collective mind while dead or incapacitated!");
                return;
            }
        }

        if (args.Length < 1)
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _chat ??= ent.System<ChatSystem>();
        _chat.TrySendInGameICMessage(mob, message, InGameICChatType.CollectiveMind, ChatTransmitRange.Normal);
    }
}
