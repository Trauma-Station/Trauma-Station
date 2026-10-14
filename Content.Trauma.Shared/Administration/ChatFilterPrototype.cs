// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Administration;

/// <summary>
/// A chat filter which can punish players for chat and mostly every text input method.
/// Punishment will always notify admins, but can vary from nothing, trolling or immediate permaban.
/// </summary>
[Prototype]
public sealed partial class ChatFilterPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The yml specified regex if it can be safely put in the repo.
    /// If it can't be put in the repo use <see cref="Cvar"/> instead.
    /// </summary>
    [DataField(required: true)]
    public string Regex;

    /// <summary>
    /// The name of the string cvar with a regex to use instead of <see cref="Regex"/>.
    /// Updates automatically when changed.
    /// </summary>
    [DataField]
    public string? Cvar;

    /// <summary>
    /// Whether to block the message from being sent.
    /// </summary>
    [DataField]
    public bool Block = true;

    /// <summary>
    /// Disables the filter without having to remove the prototype.
    /// </summary>
    [DataField]
    public bool Disabled;

    /// <summary>
    /// Whether this filter applies to chat messages and such.
    /// </summary>
    [DataField]
    public bool ApplyToChat = true;

    /// <summary>
    /// Whether this filter applies to names.
    /// </summary>
    [DataField]
    public bool ApplyToName;

    /// <summary>
    /// The first line of the ban reason to show for an automated permaban.
    /// This is also used for the webhook message, which doesn't get the offending message added.
    /// </summary>
    [DataField]
    public string? BanMessage;

    /// <summary>
    /// Effects to apply to the player's mob.
    /// </summary>
    [DataField]
    public EntityEffect[]? Effects;
}
