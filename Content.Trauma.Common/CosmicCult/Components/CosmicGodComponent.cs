using Robust.Shared.GameStates;

namespace Content.Trauma.Common.CosmicCult.Components;

/// <summary>
/// Marker component for The Unknown. We also use this to detect its spawn through CultRule!
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicGodComponent : Component;
