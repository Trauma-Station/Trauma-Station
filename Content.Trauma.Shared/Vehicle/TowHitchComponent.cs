using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Vehicle;

/// <summary>
/// Component for vehicles that can carry tow-hitch compatible objects behind them.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TowHitchComponent : Component
{
    /// <summary>
    /// A whitelist of what can be tow-hitched.
    /// </summary>
    /// <remarks>
    /// This is required because I don't trust people not abusing this
    /// </remarks>
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    /// <summary>
    /// The entity we have on our tow-hitch
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Hitching;

    /// <summary>
    /// A given offset, which is added to <see cref="Hitching"/> when parented.
    /// For visuals, mostly.
    /// </summary>
    [DataField]
    public Vector2 Offset;
}
