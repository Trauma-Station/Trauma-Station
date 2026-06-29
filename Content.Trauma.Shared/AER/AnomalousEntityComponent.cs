using System.Xml;
using Content.Trauma.Shared.Circuits;

namespace Content.Trauma.Shared.AER;

/// <summary>
/// identifies anomalous entities that can be contained for research points
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnomalousEntityComponent : Component
{
    /// <summary>
    /// quantity of research per second produced when contained
    /// </summary>
    [DataField]
    public int ResearchPerSecond = new();
    /// <summary>
    /// if the entity is contained
    /// </summary>
    [DataField]
    public bool Contained = new();
    /// <summary>
    /// quantity of research obtained when interacting with the entity
    /// </summary>
    [DataField]
    public float ResearchOnBehaviour = new();

    //containment sensor connected to the entity
    [ViewVariables]
    public EntityUid? ConnectedContainment;

}
