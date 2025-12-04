// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Adds or removes components from the target mob when mutated and reverts it when removed.
/// </summary>
[RegisterComponent, Access(typeof(ComponentMutationSystem))]
public sealed partial class ComponentMutationComponent : Component
{
    /// <summary>
    /// Components added to the mutated entity when active.
    /// WARNING: If these already exist before being added the old data is forgotten and will be removed if it gets deactivated.
    /// </summary>
    [DataField]
    public ComponentRegistry? Added;

    /// <summary>
    /// Components removed from the mutated entity when active.
    /// WARNING: These do not have their data re-added, only the default component!
    /// </summary>
    [DataField]
    public ComponentRegistry? Removed;
}
