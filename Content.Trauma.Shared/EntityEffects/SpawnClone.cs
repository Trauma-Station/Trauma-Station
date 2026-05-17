// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cloning;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that spawns a clone of a target, at the target's location.
/// </summary>
public sealed partial class SpawnClone : EntityEffectBase<SpawnClone>
{
    /// <summary>
    /// The settings to apply to the clone.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CloningSettingsPrototype> Settings;

    /// <summary>
    /// If non-null, it will also apply these extra components.
    /// </summary>
    [DataField]
    public ComponentRegistry? ComponentsToAdd;

    /// <summary>
    /// If non-null, it will also remove these extra components.
    /// </summary>
    [DataField]
    public ComponentRegistry? ComponentsToRemove;
}
