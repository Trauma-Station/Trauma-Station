// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Emoting;

[RegisterComponent, NetworkedComponent]
public sealed partial class CounterStatusEffectComponent : Component
{
    [DataField]
    public int Count;
}
