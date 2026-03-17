using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._FarHorizons.GenericFieldGenerator.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GenericFieldComponent : Component
{
    /// <summary>
    /// What made this entity?
    /// </summary>
    [ViewVariables]
    public Entity<GenericFieldGeneratorComponent>? SourceGen;

    /// <summary>
    /// was a temporary tile made with this entity?
    /// </summary>
    [ViewVariables]
    public bool TempTile = false;

    /// <summary>
    /// what tile was made with the entity?
    /// </summary>
    [ViewVariables]
    public TileRef Tileref;

    /// <summary>
    /// MapGrid for tile that was made with the entity
    /// </summary>
    [ViewVariables]
    public MapGridComponent MapGrid;

    /// <summary>
    /// GridUid for tile that was made with the entity
    /// </summary>
    [ViewVariables]
    public EntityUid GridUid;

    /// <summary>
    /// how much damage to heal per second
    /// </summary>
    [ViewVariables]
    public float RegenRate = -1f;

    /// <summary>
    /// Used to check if it's healed damage recently.
    /// </summary>
    [DataField("accumulator")]
    public float Accumulator;

    /// <summary>
    /// How many seconds should the field wait to regenerate?
    /// </summary>
    [DataField("threshold")]
    public float Threshold = 0.2f;
}