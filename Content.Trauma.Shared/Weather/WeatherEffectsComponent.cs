// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Areas;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Weather;

/// <summary>
/// Component for weather status effects that applies entity effects to anything unprotected.
/// </summary>
// TODO: change this to areas in the ash storm prototype if every lavaland ruin etc gets updated
[RegisterComponent, NetworkedComponent, Access(typeof(WeatherEffectsSystem))]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class WeatherEffectsComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

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
