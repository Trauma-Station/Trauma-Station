// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Utility;

namespace Content.Trauma.Common.Knowledge;

[Serializable, NetSerializable]
public record struct SkillInfo(string Name, string Description, Color Color, SpriteSpecifier? Sprite, int LearnedLevel, int NetLevel, int CurrentExp, int ExpCost);

[Serializable, NetSerializable]
public record struct AttributeInfo(string Name, string Description, NetEntity Entity);
