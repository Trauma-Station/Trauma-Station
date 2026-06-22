// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.LinkAccount;

[Serializable, NetSerializable]
public sealed record SharedRMCLobbyMessage(string Message)
{
    public const int CharacterLimit = 40;
}
