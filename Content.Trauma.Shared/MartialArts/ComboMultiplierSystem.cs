// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Physics.Components;

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// Calculates the multiplier for each kind of <see cref="BaseComboMultiplierEvent"/>.
/// </summary>
public sealed partial class ComboMultiplierSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnFlat(FlatMultiplierEvent args)
        => args.Multiplier = args.Value;

    [SubscribeLocalEvent]
    private void OnVelocity(EntityUid uid, PhysicsComponent comp, VelocityMultiplierEvent args)
        => args.Multiplier = Math.Clamp(MathF.Pow(comp.LinearVelocity.Length(), args.Exponent), args.Min, args.Max);
}
