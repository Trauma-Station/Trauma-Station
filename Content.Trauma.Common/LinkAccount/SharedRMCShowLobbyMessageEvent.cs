// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.LinkAccount;

[Serializable, NetSerializable]
public sealed class SharedRMCShowLobbyMessageEvent(string text) : EntityEventArgs
{
    public readonly string Text = text;
}
