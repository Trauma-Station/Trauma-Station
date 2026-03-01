using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._DV.CosmicCult.Components;

/// <summary>
/// Applies visuals to high-level cultists after using an influence
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CosmicMalignEchoComponent : Component
{
    [DataField]
    public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(new("/Textures/_DV/CosmicCult/Effects/ascendantinfection.rsi"), "vfx");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ExpireTimer = default!;
}

[Serializable, NetSerializable]
public enum CosmicEchoKey : byte
{
    Key
}
