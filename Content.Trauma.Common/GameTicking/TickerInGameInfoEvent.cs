// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.GameTicking;

/// <summary>
/// In-game info similar to the lobby info but for separated game HUD.
/// </summary>
[Serializable, NetSerializable]
public sealed class TickerInGameInfoEvent(string text) : EntityEventArgs
{
    public readonly string Text = text;
}
