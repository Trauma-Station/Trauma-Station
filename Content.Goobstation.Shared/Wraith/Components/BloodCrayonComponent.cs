// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Wraith.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCrayonComponent : Component
{
    /// <summary>
    /// Wraith Points to consume on use
    /// </summary>
    [DataField(required: true)]
    public int WpConsume;
}
