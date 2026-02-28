// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.Clothing.Components;

[RegisterComponent]
public sealed partial class MadnessMaskComponent : Component
{
    public float UpdateAccumulator = 0f;
    [DataField] public float UpdateTimer = 1f;
}
