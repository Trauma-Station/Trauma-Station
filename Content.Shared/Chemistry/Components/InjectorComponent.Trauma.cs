// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Chemistry.Components;

public sealed partial class InjectorComponent
{
    [DataField]
    public float? InteractionRangeOverride;

    [DataField]
    public bool BreakOnMove = true;
}
