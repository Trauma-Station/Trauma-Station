// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Language;

namespace Content.Trauma.Shared.Language;

/// <summary>
/// Sends spoken chat messages to every player that can understand a certain language.
/// Whispering and non-target languages are ignored.
/// Can be added to the body or the mind.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LanguageHivemindComponent : Component
{
    /// <summary>
    /// The language to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<LanguagePrototype> Language;

    /// <summary>
    /// The channel name to show in chat messages.
    /// </summary>
    [DataField]
    public LocId ChannelName = "chat-manager-xeno-hivemind-channel-name";
}
