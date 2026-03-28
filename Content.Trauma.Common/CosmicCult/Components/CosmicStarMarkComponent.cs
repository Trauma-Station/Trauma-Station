using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Trauma.Common.CosmicCult.Components;

/// <summary>
/// Component for revealing cosmic cultists to the crew.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class CosmicStarMarkComponent : Component
{
    [DataField]
    public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(new("/Textures/_Trauma/CosmicCult/Effects/cultrevealed.rsi"), "default");
}

[Serializable, NetSerializable]
public enum CosmicRevealedKey
{
    Key
}
