// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.BurnableFood;

[RegisterComponent]
public sealed partial class BurnableFoodComponent : Component
{
    /// <summary>
    /// The tempreture at which the entity will turn into the entity listed <see cref="BurnedFoodPrototype"/>.
    /// </summary>
    [DataField]
    public float BurnTemp = 450f;

    /// <summary>
    /// The prototype that food burns into.
    /// </summary>
    [DataField]
    public EntProtoId BurnedFoodPrototype = "FoodBurned";

    /// <summary>
    /// The prefix that will be added to the burned entity name.
    /// </summary>
    [DataField]
    public LocId BurnedPrefix = "burned-name-text";

    /// <summary>
    /// The prefix that will be added to the burned entity name.
    /// </summary>
    [DataField]
    public LocId BurnedPopup = "burned-popup-text";

    /// <summary>
    /// The sound played when burning the food.
    /// </summary>
    [DataField]
    public SoundSpecifier? BurnSound = new SoundPathSpecifier("/Audio/Effects/sizzle.ogg");
}
