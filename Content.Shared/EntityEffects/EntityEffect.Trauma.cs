// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.EntityEffects;

/// <summary>
/// Trauma - add ScaleProbability
/// </summary>
public abstract partial class EntityEffect
{
    /// <summary>
    /// If true, probability will be increased/decreased by effect scale.
    /// </summary>
    [DataField]
    public bool ScaleProbability;
}
