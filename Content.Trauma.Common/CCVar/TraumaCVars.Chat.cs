// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    /// <summary>
    /// Whether to play a sound when a highlighted message is received.
    /// </summary>
    public static readonly CVarDef<bool> ChatHighlightSound =
        CVarDef.Create("chat.highlight_sound", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Volume of the highlight sound when a highlighted message is received.
    /// </summary>
    public static readonly CVarDef<float> ChatHighlightVolume =
        CVarDef.Create("chat.highlight_volume", 1f, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// You get instantly banned if you say something matching this regex in any chat channel.
    /// Used by the <c>GamerWords</c> chat filter.
    /// </summary>
    public static readonly CVarDef<string> GamerWordsRegex =
        CVarDef.Create("chat.gamer_words_regex", string.Empty, CVar.SERVER | CVar.CONFIDENTIAL);

    /// <summary>
    /// You get maimed and noted if you say something matching this regex in any chat channel.
    /// Used by the <c>WordsThatKill</c> chat filter.
    /// </summary>
    public static readonly CVarDef<string> KillRegex =
        CVarDef.Create("chat.kill_regex", string.Empty, CVar.SERVER | CVar.CONFIDENTIAL);
}
