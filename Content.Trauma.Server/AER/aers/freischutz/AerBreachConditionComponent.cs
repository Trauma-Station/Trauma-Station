namespace Content.Trauma.Server.Aer.Objectives;

/// <summary>
/// checks if the entity is connected to a aer sensor and in range (contained)
/// </summary>
[RegisterComponent, Access(typeof(AerBreachConditionSystem))]
public sealed partial class AerBreachConditionComponent : Component
{
}