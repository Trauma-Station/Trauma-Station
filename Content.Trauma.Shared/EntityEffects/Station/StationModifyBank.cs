// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects.Station;

/// <summary>
/// Station effect that adds a random amount to a bank account's balance.
/// </summary>
public sealed partial class StationModifyBank : EntityEffectBase<StationModifyBank>
{
    /// <summary>
    /// The account to modify.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CargoAccountPrototype> Account;

    [DataField(required: true)]
    public int Min;

    [DataField(required: true)]
    public int Max;

    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}

public sealed partial class StationModifyBankSystem : EntityEffectSystem<StationBankAccountComponent, StationModifyBank>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedCargoSystem _cargo = default!;

    protected override void Effect(Entity<StationBankAccountComponent> ent, ref EntityEffectEvent<StationModifyBank> args)
    {
        var e = args.Effect;
        var money = _random.Next(e.Min, e.Max);
        _cargo.TryAdjustBankAccount(ent.AsNullable(), e.Account, money);
    }
}
