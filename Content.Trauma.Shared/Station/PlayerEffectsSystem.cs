// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Station;

/// <summary>
/// Runs entity effects for station players after they spawn.
/// </summary>
public sealed class PlayerEffectsSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnCompleted);
    }

    private void OnSpawnCompleted(PlayerSpawnCompleteEvent args)
    {
        var uid = args.Mob;
        foreach (var proto in _proto.EnumeratePrototypes<PlayerEffectsPrototype>())
        {
            if (_conditions.TryConditions(uid, proto.Conditions))
                _effects.ApplyEffects(uid, proto.Effects);
        }
    }
}
