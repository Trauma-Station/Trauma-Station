// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

/// <summary>
/// Controls transmission of chat.
/// </summary>
public enum ChatTransmitRange : byte
{
    /// Acts normal, ghosts can hear across the map, etc.
    Normal,
    /// Normal but ghosts are still range-limited.
    GhostRangeLimit,
    /// Hidden from the chat window.
    HideChat,
    /// Ghosts can't hear or see it at all. Regular players can if in-range.
    NoGhosts
}

/// <summary>
/// InGame IC chat is for chat that is specifically ingame (not lobby) but is also in character, i.e. speaking.
/// </summary>
// ReSharper disable once InconsistentNaming
[Serializable, NetSerializable]
public enum InGameICChatType : byte
{
    Speak,
    Emote,
    Whisper,
    // <Goob>
    CollectiveMind
    // </Goob>
}

/// <summary>
/// InGame OOC chat is for chat that is specifically ingame (not lobby) but is OOC, like deadchat or LOOC.
/// </summary>
[Serializable, NetSerializable]
public enum InGameOOCChatType : byte
{
    Looc,
    Dead
}
