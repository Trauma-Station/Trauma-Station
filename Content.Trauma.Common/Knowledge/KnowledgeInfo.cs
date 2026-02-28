// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Trauma.Common.Knowledge;

[Serializable, NetSerializable]
public record struct KnowledgeInfo(string Name, string Description, Color Color, SpriteSpecifier? Sprite);
