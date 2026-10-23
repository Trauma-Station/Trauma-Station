// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Goobstation.Shared.Wraith.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HauntComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionHaunt";

    [DataField]
    public EntityUid? ActionEnt;

    /// <summary>
    /// How much the Wp regeneration gets boosted per witness.
    /// </summary>
    [DataField]
    public FixedPoint2 HauntWpRegenPerWitness = 1;

    /// <summary>
    /// How long the Wp regen boost lasts.
    /// </summary>
    [DataField]
    public TimeSpan HauntWpRegenDuration = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan NextHauntWpRegenUpdate = TimeSpan.Zero;

    /// <summary>
    /// How long the Wraith remains corporeal while haunting.
    /// </summary>
    [DataField]
    public TimeSpan HauntCorporealDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long the flash effect lasts when someone gets haunted.
    /// </summary>
    [DataField]
    public TimeSpan HauntFlashDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Is the action active?
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Is the wp boost active?
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool WpBoostActive;

    /// <summary>
    /// How long the haunt lasts
    /// </summary>
    [DataField]
    public TimeSpan HauntDuration = TimeSpan.FromSeconds(30);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextHauntUpdate;

    [DataField]
    public TimeSpan WitnessUpdate = TimeSpan.FromSeconds(0.5f);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan WitnessNextUpdate;

    /// <summary>
    /// Total WP regeneration added by Haunt
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 HauntWpRegenApplied;
}
