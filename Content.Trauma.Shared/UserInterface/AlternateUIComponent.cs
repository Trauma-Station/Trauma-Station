// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.UserInterface;

/// <summary>
/// Adds an alt verb to open a BUI separate from the main ActivatableUI.
/// Shares the UI opening requirements, held items, etc.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AlternateUISystem))]
public sealed partial class AlternateUIComponent : Component
{
    /// <summary>
    /// The alternate UI to open when requested.
    /// </summary>
    [DataField(required: true)]
    public Enum Key;

    /// <summary>
    /// The text used in the verb.
    /// </summary>
    [DataField(required: true)]
    public string VerbText;
}
