// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eui;

namespace Content.Goobstation.Shared.ServerCurrency.UI;

[Serializable, NetSerializable]
public sealed class CurrencyEuiState : EuiStateBase;

public static class CurrencyEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase;

    [Serializable, NetSerializable]
    public sealed class Buy : EuiMessageBase
    {
        public ProtoId<TokenListingPrototype> TokenId;
    }
}
