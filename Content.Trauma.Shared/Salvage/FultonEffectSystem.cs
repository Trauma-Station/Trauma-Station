// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Salvage.Fulton;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Trauma.Shared.Salvage;

/// <summary>
/// Predicts fulton effect spawn/despawn and adding a status effect while fultoned.
/// </summary>
public sealed class FultonEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public static readonly EntProtoId StatusEffect = "BeingFultonedStatusEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FultonedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FultonedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<FultonedComponent> ent, ref ComponentStartup args)
    {
        if (_timing.ApplyingState)
            return;

        _status.TryAddStatusEffect(ent, StatusEffect, out _);

        if (Exists(ent.Comp.Effect))
            return;

        var coords = new EntityCoordinates(ent, Vector2.Zero);
        ent.Comp.Effect = PredictedSpawnAttachedTo(SharedFultonSystem.EffectProto, coords);
        Dirty(ent);
    }

    private void OnShutdown(Entity<FultonedComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        _status.TryRemoveStatusEffect(ent, StatusEffect);

        PredictedDel(ent.Comp.Effect);
        ent.Comp.Effect = EntityUid.Invalid;
    }
}
