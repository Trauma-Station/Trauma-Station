using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.BurnableFood;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BurnableFoodComponent : Component
{
    /// <summary>
    /// The tempreture at which the entity will turn into the entity listed <see cref="BurnedFoodPrototype"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BurnTemp = 450f;

    /// <summary>
    /// The prototype that food burns into.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId BurnedFoodPrototype = "FoodBurned";

    /// <summary>
    /// The prefix that will be added to the burned entity name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BurnedPrefix = "burned-name-text";

    /// <summary>
    /// The prefix that will be added to the burned entity name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BurnedPopup = "burned-popup-text";
}
