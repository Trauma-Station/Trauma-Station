using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Trauma.Shared.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BulletholeComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<(Vector2 LocalOffset, int Seed)> HolePositions = new();

    public const int MaxHoles = 50;
}
