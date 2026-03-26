using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Temperature;

/// <summary>
/// Heats entities inside an item slot
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ItemSlotHeaterComponent : Component
{
    /// <summary>
    /// The slot to heat
    /// </summary>
    [DataField(required: true)]
    public string Slot;

    /// <summary>
    /// The temperature to heat the entities
    /// </summary>
    [DataField]
    public float Temp = 100f;

    /// <summary>
    /// The max temperature of the item inside
    /// </summary>
    [DataField]
    public float MaxTemp = 300f;

    /// <summary>
    /// How often to update the heating
    /// </summary>
    [DataField]
    public TimeSpan Update = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdate;
}
