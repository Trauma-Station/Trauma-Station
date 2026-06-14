// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;

namespace Content.Trauma.Shared.Vampires;

/// <summary>
/// This action performs a stun on all sides of the performer.
/// Depending on the <see cref="Deviation"/> of the target from our performer, different Entity Effects will be applied.
///
/// The "sides" of our performer are not unique, therefore they are bundled together as <see cref="Deviation.Partial"/> (you can't do different effects for each side).
///
/// If the performer uses this ability while they are stunned, only the <see cref="Deviation.Partial"/> Entity Effects apply to the targets.
/// </summary>
public sealed partial class VampireGlareSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<StunnedComponent> _stunnedQuery = default!;
    [Dependency] private EntityQuery<LimitedChargesComponent> _chargesQuery = default!;

    private HashSet<Entity<StatusEffectsComponent>> _target = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionVampireGlareComponent, VampireGlareEvent>(OnGlare);
    }

    private void OnGlare(Entity<ActionVampireGlareComponent> ent, ref VampireGlareEvent args)
    {
        var performer = args.Performer;

        // Check if we are blindfolded
        var ev = new CanSeeAttemptEvent();
        RaiseLocalEvent(performer, ev);
        if (ev.Blind)
        {
            _popup.PopupClient("You can't use glare while blinded!", performer, PopupType.LargeCaution);
            return;
        }

        if (!_chargesQuery.TryComp(args.Action, out var limitedCharges))
            return;

        // In our case, full stun should take place when spamming 2 charges together, therefore we must scale all the effects at 0.5 (if we have 2 max charges)
        var scale = limitedCharges.MaxCharges / 4f;
        var xform = Transform(performer);
        var mapCoords = _transform.GetMapCoordinates(performer);
        var isStunned = _stunnedQuery.HasComponent(performer);

        var range = ent.Comp.Range;
        var sideEffects = ent.Comp.SideEffects;
        var frontEffects = ent.Comp.FrontEffects;
        var behindEffects = ent.Comp.BehindEffects;

        _target.Clear();
        _lookup.GetEntitiesInRange(mapCoords, range, _target);
        foreach (var target in _target)
        {
            if (target.Owner == performer)
                continue;

            var attemptEv = new GlareAttemptEvent();
            RaiseLocalEvent(target, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            if (isStunned)
            {
                _effects.ApplyEffects(target, sideEffects, scale);
                continue;
            }

            var deviation = CalculateDeviation(xform, Transform(target));
            switch (deviation)
            {
                case Deviation.Full:
                {
                    _effects.ApplyEffects(target, behindEffects, scale);
                    break;
                }
                case Deviation.Partial:
                {
                    _effects.ApplyEffects(target, sideEffects, scale);
                    break;
                }
                case Deviation.None:
                {
                    _effects.ApplyEffects(target, frontEffects, scale);
                    break;
                }
            }
        }

        args.Handled = true;
    }

    #region Helper
    /// <summary>
    /// Calculates the <see cref="Deviation"/> between 2 entities.
    /// </summary>
    /// <returns>The <see cref="Deviation"/> that resulted.</returns>
    private Deviation CalculateDeviation(TransformComponent user, TransformComponent target)
    {
        var userPos = _transform.GetWorldPosition(user);
        var targetPos = _transform.GetWorldPosition(target);
        if ((targetPos - userPos).LengthSquared() < 0.1f)
           return Deviation.None;

        var userForward = _transform.GetWorldRotation(user).ToWorldVec();
        var toTarget = (targetPos - userPos).Normalized();
        var dot = Vector2.Dot(userForward, toTarget);

        if (dot >= 0.7f)
            return Deviation.None;

        if (dot <= -0.7f)
            return Deviation.Full;

        return Deviation.Partial;
    }
    #endregion
}

/// <summary>
/// Deviation just means the amount of which a measurement is different from another amount.
///
/// In short;
/// - None means our target is ahead of us.
/// - Partial means our target is on our sides.
/// - Full means our target is behind us.
/// </summary>
public enum Deviation : byte
{
    None,
    Partial,
    Full
}

/// <summary>
/// Raised on the target before a glare happens, in case we want to cancel it for them.
/// </summary>
[ByRefEvent]
public record struct GlareAttemptEvent(bool Cancelled = false);
