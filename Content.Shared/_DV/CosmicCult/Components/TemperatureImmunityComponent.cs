using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Hides temperature alerts and prevents damage while added to an entity.
/// Force added and removed by cult conversion.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TemperatureImmunityComponent : Component;
