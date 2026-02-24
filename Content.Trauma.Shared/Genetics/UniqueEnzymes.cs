// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Genetics;

/// <summary>
/// Basic appearance features that can be applied to a Mutatable mob.
/// Hair isn't included since you can do that with barber scissors / mirror.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public partial record struct UniqueEnzymes(string Name, string? Prints, Sex? Sex, Gender? Gender, Color? EyeColor, Color? SkinColor);
