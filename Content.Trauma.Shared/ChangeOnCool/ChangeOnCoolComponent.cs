using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.ChangeOnCool;

[RegisterComponent]
public sealed partial class ChangeOnCoolComponent : Component
{
    /// <summary>
    /// The temperature at which the entity is replaced the cooled version.
    /// </summary>
    [DataField]
    public float CoolTemp = 100f;

    /// <summary>
    /// The new entity that replaces the cooled entity.
    /// </summary>
    [DataField]
    public EntProtoId CooledPrototype = "FoodBurned";

    /// <summary>
    /// The popup when the entity reaches the cooled temperature.
    /// </summary>
    [DataField]
    public LocId CooledPopup = "burned-popup-text";
}
