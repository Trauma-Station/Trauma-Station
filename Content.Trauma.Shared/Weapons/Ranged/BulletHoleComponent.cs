// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Trauma.Shared.Weapons.Ranged;

/// <summary>
/// Anything with this components will create bullet holes when a projectile with a bulletholegenerator component
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BulletHoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<Vector2> HolePositions = new();

    public const int MaxHoles = 50;
}
