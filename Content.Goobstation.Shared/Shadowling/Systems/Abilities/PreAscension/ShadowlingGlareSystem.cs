// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Lumminal <81829924+Lumminal@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Roudenn <romabond091@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Shadowling.Components.Abilities.PreAscension;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Speech.Muting;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Shadowling.Systems.Abilities.PreAscension;

/// <summary>
/// This handles the Glare ability
/// </summary>
public sealed class ShadowlingGlareSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedShadowlingSystem _shadowling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StatusEffectsSystem _effects = default!;
    [Dependency] private readonly Content.Shared.StatusEffectNew.StatusEffectsSystem _effectsNew = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowlingGlareComponent, GlareEvent>(OnGlare);
        SubscribeLocalEvent<ShadowlingGlareComponent, MapInitEvent>(OnStartup);
        SubscribeLocalEvent<ShadowlingGlareComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<ShadowlingGlareComponent> ent, ref MapInitEvent args)
        => _actions.AddAction(ent.Owner, ref ent.Comp.ActionEnt, ent.Comp.ActionId);

    private void OnShutdown(Entity<ShadowlingGlareComponent> ent, ref ComponentShutdown args)
        => _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<ShadowlingGlareComponent>();
        var decay = TimeSpan.FromSeconds(frameTime);
        while (query.MoveNext(out _, out var glare))
        {
            if (glare.ActivateGlareTimer)
            {
                // Time before the ability activates
                glare.GlareTimeBeforeEffect -= decay;

                if (glare.GlareTimeBeforeEffect <= TimeSpan.Zero)
                    ActivateStun(glare.GlareTarget, glare);
            }
        }
    }

    private void ActivateStun(EntityUid target, ShadowlingGlareComponent comp)
    {
        _stun.TryAddParalyzeDuration(target, comp.GlareStunTime);
        comp.ActivateGlareTimer = false;
    }

    private void OnGlare(EntityUid uid, ShadowlingGlareComponent comp, GlareEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        var user = args.Performer;

        if (!_shadowling.CanGlare(target))
            return;

        var targetCoords = _transform.GetWorldPosition(target);
        var distance = (_transform.GetWorldPosition(user) - targetCoords).Length();
        comp.GlareTarget = target;

        if (distance <= comp.MinGlareDistance)
        {
            comp.GlareStunTime = comp.MaxGlareStunTime;
            _stun.TryUpdateParalyzeDuration(target, comp.GlareStunTime);
        }
        else
        {
            // Do I know what is going on here? No. But it works so... Thanks for listening!
            comp.GlareStunTime *= (1 - Math.Clamp(distance / comp.MaxGlareDistance, 0, 1));
            comp.GlareTimeBeforeEffect = comp.MinGlareDelay + (comp.MaxGlareDelay - comp.MinGlareDelay) * Math.Clamp(distance / comp.MaxGlareDistance, 0, 1);

            comp.ActivateGlareTimer = true;
        }

        // Glare mutes and slows down the target no matter what.
        _effects.TryAddStatusEffect<MutedComponent>(target, "Muted", comp.MuteTime, true);
        _effectsNew.TryUpdateStatusEffectDuration(target, comp.SlowdownStatusEffect, comp.SlowTime);

        var effectEnt = PredictedSpawnAtPosition(comp.EffectGlare, Transform(uid).Coordinates);
        _transform.SetParent(effectEnt, uid);

        _popup.PopupEntity(Loc.GetString("shadowling-glare-target"), target, target, PopupType.MediumCaution);
        args.Handled = true;
    }
}
