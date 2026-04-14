// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Areas;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Weather;

/// <summary>
/// Component for weather status effects that damages anything not protected from this weather which does not match a blacklist.
/// </summary>
// TODO: change this to areas in the ash storm prototype if every lavaland ruin etc gets updated
[RegisterComponent, NetworkedComponent, Access(typeof(WeatherDamageSystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class WeatherDamageComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Optional blacklist for entities which do not get damaged.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// If true, being under a roof makes you safe from damage.
    /// </summary>
    [DataField]
    public bool SafeIndoors = true;

    /// <summary>
    /// Areas that are safe to be inside.
    /// </summary>
    [DataField]
    public List<EntProtoId<AreaComponent>> SafeAreas = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// How long to wait between each damage cycle.
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);
}
