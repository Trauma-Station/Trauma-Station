// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CosmicTrailComponent : Component
{
    [DataField]
    public float CosmicFieldRadius = 0.5f;

    [DataField]
    public float CosmicFieldLifetime = 5f;

    [DataField]
    public int Strength;

    [DataField, AutoPausedField]
    public TimeSpan NextCosmicFieldTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan CosmicFieldPeriod = TimeSpan.FromSeconds(0.1f);
}
