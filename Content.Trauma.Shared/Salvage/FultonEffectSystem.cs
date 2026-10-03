// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Popups;
using Content.Shared.Salvage.Fulton;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Salvage;

/// <summary>
/// Predicts fulton effect spawn/despawn and adding a status effect/alert while fultoned.
/// </summary>
public sealed partial class FultonEffectSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    public static readonly EntProtoId StatusEffect = "BeingFultonedStatusEffect";
    public static readonly ProtoId<AlertPrototype> Alert = "Fultoned";

    [SubscribeLocalEvent]
    private void OnStartup(Entity<FultonedComponent> ent, ref ComponentStartup args)
    {
        if (_timing.ApplyingState)
            return;

        _alerts.ShowAlert(ent.Owner, Alert);
        _status.TryAddStatusEffect(ent, StatusEffect, out _);

        if (Exists(ent.Comp.Effect))
            return;

        var coords = new EntityCoordinates(ent, Vector2.Zero);
        ent.Comp.Effect = PredictedSpawnAttachedTo(SharedFultonSystem.EffectProto, coords);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<FultonedComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        _alerts.ClearAlert(ent.Owner, Alert);
        _status.TryRemoveStatusEffect(ent, StatusEffect);

        PredictedDel(ent.Comp.Effect);
        ent.Comp.Effect = EntityUid.Invalid;
    }

    [SubscribeLocalEvent]
    private void OnAlertClicked(Entity<FultonedComponent> ent, ref RemoveFultonAlertEvent args)
    {
        if (!_blocker.CanInteract(ent, target: null))
            return;

        if (!ent.Comp.Removeable)
        {
            _popup.PopupEntity("You aren't able to remove the fulton!", ent, ent, PopupType.SmallCaution);
            return;
        }

        _popup.PopupEntity("You detach the fulton from yourself.", ent, ent);
        RemCompDeferred(ent, ent.Comp);
    }
}

public sealed partial class RemoveFultonAlertEvent : BaseAlertEvent;
