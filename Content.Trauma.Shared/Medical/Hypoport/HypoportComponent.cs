using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Medical.Hypoport;

/// <summary>
/// Component for a hypoport assembly item, which allows hyposprays to inject into the body when installed via surgery or trait.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HypoportComponent : Component;

/// <summary>
/// Event raised on a hypoport assembly item to check if a user is allowed to inject us.
/// The system cancelling this should show a popup to the user explaining why it failed.
/// </summary>
[ByRefEvent]
public record struct HypoportInjectAttemptEvent(EntityUid Target, EntityUid User, EntityUid Used, bool Cancelled = false, LocId? InjectMessageOverride = null);
