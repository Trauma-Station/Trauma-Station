// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Mail;

/// <summary>
/// A placeholder for another entity, spawned when dropped or placed in someone's hands.
/// Useful for storing instant effect entities, e.g. smoke, in the mail.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DelayedItemComponent : Component
{
    /// <summary>
    /// The entity to replace this when opened or dropped.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Item;
}
