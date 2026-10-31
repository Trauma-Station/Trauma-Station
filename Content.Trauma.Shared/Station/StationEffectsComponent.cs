// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Station;

/// <summary>
/// Station component that runs effect on the map
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationEffectsComponent : Component
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
