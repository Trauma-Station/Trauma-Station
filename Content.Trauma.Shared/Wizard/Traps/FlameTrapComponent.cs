// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Wizard.Traps;

[RegisterComponent]
public sealed partial class FlameTrapComponent : Component
{
    [DataField]
    public float FireStacks = 6f;
}
