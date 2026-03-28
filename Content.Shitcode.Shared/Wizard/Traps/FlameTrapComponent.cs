// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shitcode.Shared.Wizard.Traps;

[RegisterComponent]
public sealed partial class FlameTrapComponent : Component
{
    [DataField]
    public float FireStacks = 6f;
}
