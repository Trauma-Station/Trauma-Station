// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    /// <summary>
    /// Discord webhook to send ban messages to.
    /// Disables ban webhooks if empty.
    /// </summary>
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("trauma.discord_ban_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Name to use for the ban webhook.
    /// </summary>
    public static readonly CVarDef<string> DiscordBanName =
        CVarDef.Create("trauma.discord_ban_name", string.Empty, CVar.SERVERONLY);

    /// <summary>
    /// URL to use for the ban webhook's avatar.
    /// </summary>
    public static readonly CVarDef<string> DiscordBanAvatar =
        CVarDef.Create("trauma.discord_ban_avatar", string.Empty, CVar.SERVERONLY);
}
