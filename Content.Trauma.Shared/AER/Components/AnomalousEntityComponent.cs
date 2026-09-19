// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

/// <summary>
/// identifies anomalous entities that can be contained for research points
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnomalousEntityComponent : Component
{
    /// <summary>
    /// quantity of research per second produced when contained
    /// </summary>
    [DataField]
    public int ResearchPerSecond;

    /// <summary>
    /// if the entity is contained
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Contained;

    /// <summary>
    /// if the entity is active (ex alive, powered, ecc.) if false stops research production
    /// </summary>
    [DataField]
    public bool Active = true;

    /// <summary>
    /// quantity of research obtained when interacting with the entity
    /// </summary>
    [DataField]
    public int ResearchOnBehaviour;

    /// <summary>
    /// protoId of the I.D. gear to spawn on behaviours
    /// </summary>
    [DataField]
    public EntProtoId? IdGear;

    /// <summary>
    /// containment sensor connected to the entity
    /// </summary>
    [DataField]
    public EntityUid? ConnectedContainment;

}
