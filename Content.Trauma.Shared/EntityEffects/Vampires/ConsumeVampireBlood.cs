// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Shared.EntityEffects.Vampires;

/// <summary>
/// Effect that consumes the usable blood off an entity with <see cref="VampireComponent"/>.
/// </summary>
public sealed partial class ConsumeVampireBlood : EntityEffectBase<ConsumeVampireBlood>
{
    /// <summary>
    ///  How much usable blood we want to subtract from the vampire?
    /// </summary>
    [DataField(required: true)]
    public int Amount;
}

public sealed partial class ConsumeVampireBloodEffectSystem : EntityEffectSystem<VampireComponent, ConsumeVampireBlood>
{
    [Dependency] private VampireSystem _vampire = default!;

    protected override void Effect(Entity<VampireComponent> ent, ref EntityEffectEvent<ConsumeVampireBlood> args)
    {
        var blood = args.Effect.Amount;

        _vampire.SubtractUsableBlood(ent.AsNullable(), blood);
    }
}
