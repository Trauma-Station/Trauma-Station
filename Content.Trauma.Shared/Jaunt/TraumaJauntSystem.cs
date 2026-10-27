// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Jaunt;
using Content.Shared.StatusEffectNew;

namespace Content.Trauma.Shared.Jaunt;

public sealed partial class TraumaJauntSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    private static readonly EntProtoId JauntMutedEffect = "StatusEffectMutedJaunt";

    [SubscribeLocalEvent]
    private void OnJauntInit(Entity<JauntComponent> ent, ref ComponentInit args)
    {
        _status.TryAddStatusEffect(ent.Owner, JauntMutedEffect, out _, null);
    }
}
