// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Speech;

/// <summary>
/// Overrides the speech font of a speaking mob.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SpeechFontOverrideSystem))]
public sealed partial class SpeechFontOverrideComponent : Component
{
    /// <summary>
    /// This must be a valid <c>FontPrototype</c> but it only exists in client so it cannot be validated.
    /// </summary>
    [DataField(required: true)]
    public string Font = string.Empty;

    /// <summary>
    /// When true, does nothing if the speech source is not this component's entity.
    /// </summary>
    [DataField]
    public bool SourceOnly = true;
}
