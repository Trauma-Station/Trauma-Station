// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Botany;

/// <summary>
/// Prevents this plant from being harvested if there are too many produce entities on the server.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlantHarvestLimitComponent : Component
{
    [DataField(required: true)]
    public int Limit;
}
