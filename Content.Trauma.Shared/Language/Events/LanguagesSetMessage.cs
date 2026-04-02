// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Language.Events;

/// <summary>
///     Sent from the client to the server when it needs to want to set his currentLanguage.
/// </summary>
[Serializable, NetSerializable]
public sealed class LanguagesSetMessage(string currentLanguage) : EntityEventArgs
{
    public string CurrentLanguage = currentLanguage;
}
