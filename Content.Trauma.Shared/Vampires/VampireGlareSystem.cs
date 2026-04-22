// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Charges.Systems;
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
public sealed class VampireGlareSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<StunnedComponent> _stunnedQuery = default!;

    private HashSet<Entity<StatusEffectsComponent>> _statusEffects = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireGlareEvent>(OnGlare);
    }

    private void OnGlare(VampireGlareEvent args)
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

        var xform = Transform(performer);
        var mapCoords = _transform.GetMapCoordinates(performer);
        var isStunned = _stunnedQuery.HasComponent(performer);

        _statusEffects.Clear();
        _lookup.GetEntitiesInRange(mapCoords, args.Range, _statusEffects);
        foreach (var target in _statusEffects)
        {
            if (target.Owner == performer)
                continue;

            if (isStunned)
            {
                _entityEffects.ApplyEffects(target, args.SideEffects);
                continue;
            }

            var deviation = CalculateDeviation(xform, Transform(target));
            switch (deviation)
            {
                case Deviation.Full:
                {
                    _entityEffects.ApplyEffects(target, args.BehindEffects);
                    break;
                }
                case Deviation.Partial:
                {
                    _entityEffects.ApplyEffects(target, args.SideEffects);
                    break;
                }
                case Deviation.None:
                {
                    _entityEffects.ApplyEffects(target, args.FrontEffects);
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
