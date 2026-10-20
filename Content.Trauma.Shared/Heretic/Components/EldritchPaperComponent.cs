// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components;

/// <summary>
/// Heretic paper that stores an item inside, created using <see cref="EldritchPaperConverterComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EldritchPaperComponent : Component
{
    [DataField]
    public string Slot = "item";
}
