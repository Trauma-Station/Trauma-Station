// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult;

[Serializable, NetSerializable]
public enum SoulShardVisualState : byte
{
    HasMind,
    Blessed,
    Sprite,
    Glow
}

[Serializable, NetSerializable]
public enum ConstructVisualsState : byte
{
    Transforming,
    Sprite,
    Glow
}

[Serializable, NetSerializable]
public enum GenericCultVisuals : byte
{
    State, // True or False
    Layer
}

[Serializable, NetSerializable]
public enum PylonVisuals : byte
{
    Activated,
    Layer
}

[Serializable, NetSerializable]
public enum PentagramKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum CultWinCondition : byte
{
    Draw,
    Win,
    Failure
}

[Serializable, NetSerializable]
public enum CultStage : byte
{
    Start,
    RedEyes,
    Pentagram
}
