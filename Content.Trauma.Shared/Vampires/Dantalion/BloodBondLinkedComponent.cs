// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.Vampires.Dantalion;

/// <summary>
/// Used to link an entity with a vampire, in order to share damage with each other.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BloodBondLinkedComponent : Component
{
    /// <summary>
    /// The vampire that we are linked to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Vampire;

    /// <summary>
    /// The range in which we are bound to the effects of Blood Link (relative to the vampire's position).
    /// </summary>
    [DataField]
    public float Range = 7f;
}

public sealed partial class BloodBondActionEvent : InstantActionEvent
{
    /// <summary>
    /// The range of the thrall lookup.
    /// </summary>
    [DataField]
    public float Range = 7f;

    /// <summary>
    /// How much blood to drain per second.
    /// </summary>
    [DataField]
    public int BloodDrain = 3;

    /// <summary>
    /// How often to drain blood from the vampire.
    /// </summary>
    [DataField]
    public TimeSpan Update = TimeSpan.FromSeconds(1f);
};
