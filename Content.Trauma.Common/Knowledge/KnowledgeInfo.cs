// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Knowledge;

[Serializable, NetSerializable]
public record struct KnowledgeInfo(string Name, string Desc, string LevelString, Color Color, SpriteSpecifier? Sprite, int LearnedLevel, int NetLevel, int CurrentExp, int ExpCost);
