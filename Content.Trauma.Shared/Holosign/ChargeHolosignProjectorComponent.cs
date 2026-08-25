// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Holosign;

/// <summary>
/// A holosign projector that uses <c>LimitedCharges</c> instead of a power cell slot.
/// If there is already a sign on the clicked tile it reclaims it for a charge instead of stacking it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(ChargeHolosignSystem))]
public sealed partial class ChargeHolosignProjectorComponent : Component
{
    /// <summary>
    /// The entity to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SignProto;

    /// <summary>
    /// Component on <see cref="SignProto"/> to check for duplicates.
    /// </summary>
    [DataField(required: true)]
    public CompName SignComponentName;

    public Type SignComponent = default!;

    /// <summary>
    /// Active holosigns we "own".
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Signs = new();
}
