// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Fishbait <Fishbait@git.ml>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 fishbait <gnesse@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Goobstation.Shared.Trigger.Triggers;

public sealed partial class TriggerOnDamageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnDamageComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<TriggerOnDamageComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not {} delta ||
            !delta.AnyPositive() ||
            delta.GetTotal() <= ent.Comp.Threshold) // don't trigger on low damage
            return;

        // TODO: PredictedRandom when it's real
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new Random(seed);
        if (!rand.Prob(ent.Comp.Probability))
            return;

        _trigger.Trigger(ent.Owner, args.Origin, ent.Comp.KeyOut);
    }
}
