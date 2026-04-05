using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[Serializable, NetSerializable]
public enum AtmosPipeLayerVisuals
{
    Sprite,
    SpriteLayers,
    DrawDepth,
}

[Serializable, NetSerializable]
public enum AtmosPipeLayer
{
    Primary,
    Secondary,
    Tertiary,
}
