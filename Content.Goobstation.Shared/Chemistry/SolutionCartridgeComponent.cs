// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;

namespace Content.Goobstation.Shared.Chemistry;

[RegisterComponent]
public sealed partial class SolutionCartridgeComponent : Component
{
    [DataField]
    public string TargetSolution = "default";

    [DataField(required: true)]
    public Solution Solution = default!;
}
