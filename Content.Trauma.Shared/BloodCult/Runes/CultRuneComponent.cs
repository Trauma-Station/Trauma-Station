// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.Damage;

namespace Content.Trauma.Shared.BloodCult.Runes;

/// <summary>
/// Component every rune prototype has.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultRuneComponent : Component
{
    [DataField(required: true)]
    public string InvokePhrase = string.Empty;

    [DataField]
    public InGameICChatType InvokeChatType = InGameICChatType.Whisper;

    [DataField]
    public int RequiredInvokers = 1;

    [DataField]
    public float RuneActivationRange = 1f;

    /// <summary>
    /// Damage dealt to the user on the rune activation.
    /// Other invokers are unaffected.
    /// </summary>
    [DataField]
    public DamageSpecifier? ActivationDamage;
}

/// <summary>
/// Event raised on a rune when a cultist tries to invoke it.
/// Set Handled if something happened, to complete the invocation.
/// A popup can also be shown to the user.
/// </summary>
[ByRefEvent]
public record struct RuneInvokeEvent(EntityUid User, HashSet<Entity<BloodCultMemberComponent>> Invokers, bool Handled = false, bool Predicted = true, string? Popup = null);

/// <summary>
/// Raised on a rune after it has been placed by a cultist.
/// </summary>
[ByRefEvent]
public record struct RunePlacedEvent(EntityUid User);
