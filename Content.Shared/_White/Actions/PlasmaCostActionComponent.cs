using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlasmaCostActionComponent : Component
{
    [DataField]
    public bool ShouldChangePlasma = true;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 50;

    /// <summary>
    /// Automatically consume plasma after the action is used.
    /// If you start a doafter or something, set this to false and consume plasma manually.
    /// </summary>
    [DataField]
    public bool Immediate = true;
}
