// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Common.Surgery.Tools;

// TODO: replace with tool qualities...
public abstract partial class BaseSurgeryToolComponent : Component
{
    [ViewVariables]
    public abstract string ToolName { get; }

    /// <summary>
    ///     Field intended for discardable or non-reusable tools.
    /// </summary>
    [DataField]
    public bool? Used { get; set; }

    /// <summary>
    ///     Multiply the step's doafter by this value.
    ///     This is per-type so you can have something that's a good scalpel but a bad retractor.
    /// </summary>
    [DataField]
    public float Speed { get; set; }
}
