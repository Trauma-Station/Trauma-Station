// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Shared.Audio;

namespace Content.Medical.Shared.Traumas;

public partial class TraumaSystem
{
    [Dependency] private EntityQuery<InternalOrganComponent> _internalQuery = default!;

    #region Event handling

    [SubscribeLocalEvent]
    private void OnOrganIntegrityChanged(Entity<InternalOrganComponent> organ, ref OrganIntegrityChangedEvent args)
    {
        if (_body.GetBody(organ.Owner) is not {} body)
            return;

        if (args.NewIntegrity < organ.Comp.IntegrityCap)
            return;

        foreach (var trauma in GetBodyTraumas(body, TraumaType.OrganDamage))
        {
            if (trauma.Comp.TraumaTarget == organ)
                RemoveTrauma(trauma);
        }
    }

    [SubscribeLocalEvent]
    private void OnOrganSeverityChanged(Entity<WoundableComponent> ent, ref OrganDamageSeverityChangedOnWoundable args)
    {
        if (_body.GetBody(ent.Owner) is not {} body ||
            args.NewSeverity < args.OldSeverity)
            return;

        _popup.PopupEntity(Loc.GetString($"popup-trauma-OrganDamage-{args.NewSeverity.ToString()}", ("part", ent)),
            body,
            body,
            PopupType.SmallCaution);

        if (args.NewSeverity != OrganSeverity.Destroyed)
            return;

        if (GetPartTraumas(ent.AsNullable(), out var traumas, TraumaType.OrganDamage))
        {
            foreach (var trauma in traumas)
            {
                if (trauma.Comp.TraumaTarget != args.Organ.Owner)
                    continue;

                RemoveTrauma(trauma);
            }
        }

        _audio.PlayPvs(args.Organ.Comp.OrganDestroyedSound, body);
        _part.RemoveOrgan(ent.Owner, args.Organ.Owner);
        PredictedQueueDel(args.Organ);
    }

    #endregion

    #region Public API
    public bool TryCreateOrganDamageModifier(Entity<InternalOrganComponent?> ent,
        FixedPoint2 severity,
        EntityUid effectOwner,
        string identifier)
    {
        if (severity == 0 || !_internalQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.IntegrityModifiers.TryAdd((identifier, effectOwner), severity))
            return false;

        //DirtyField(ent, ent.Comp, nameof(InternalOrganComponent.IntegrityModifiers));
        UpdateOrganIntegrity(ent);

        return true;
    }

    public bool TryChangeOrganDamageModifier(Entity<InternalOrganComponent?> ent,
        FixedPoint2 change,
        EntityUid effectOwner,
        string identifier)
    {
        if (change == 0 || !_internalQuery.Resolve(ent, ref ent.Comp))
            return false;

        var key = (identifier, effectOwner);
        if (!ent.Comp.IntegrityModifiers.TryGetValue(key, out var value))
            return false;

        ent.Comp.IntegrityModifiers[key] = value + change;
        //DirtyField(ent, ent.Comp, nameof(InternalOrganComponent.IntegrityModifiers));
        UpdateOrganIntegrity(ent);

        return true;
    }

    public bool TryRemoveOrganDamageModifier(Entity<InternalOrganComponent?> ent,
        EntityUid effectOwner,
        string identifier)
    {
        if (!_internalQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.IntegrityModifiers.Remove((identifier, effectOwner)))
            return false;

        //DirtyField(ent, ent.Comp, nameof(InternalOrganComponent.IntegrityModifiers));

        if (_traumaQuery.TryComp(effectOwner, out var trauma))
            RemoveTrauma((effectOwner, trauma));

        UpdateOrganIntegrity(ent);
        return true;
    }

    #endregion

    #region Private API

    private void UpdateOrganIntegrity(Entity<InternalOrganComponent?> ent)
    {
        if (!_internalQuery.Resolve(ent, ref ent.Comp))
            return;

        var oldIntegrity = ent.Comp.OrganIntegrity;

        if (ent.Comp.IntegrityModifiers.Count > 0)
            ent.Comp.OrganIntegrity = FixedPoint2.Clamp(ent.Comp.IntegrityModifiers
                .Aggregate(FixedPoint2.Zero, (current, modifier) => current + modifier.Value),
                0,
                ent.Comp.IntegrityCap);

        if (oldIntegrity == ent.Comp.OrganIntegrity)
            return;

        DirtyField(ent, ent.Comp, nameof(InternalOrganComponent.OrganIntegrity));

        var ev = new OrganIntegrityChangedEvent(oldIntegrity, ent.Comp.OrganIntegrity);
        RaiseLocalEvent(ent, ref ev);

        var nearestSeverity = ent.Comp.OrganSeverity;
        foreach (var (severity, value) in ent.Comp.IntegrityThresholds.OrderByDescending(kv => kv.Value))
        {
            if (ent.Comp.OrganIntegrity < value)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity == ent.Comp.OrganSeverity)
            return;

        ent.Comp.OrganSeverity = nearestSeverity;
        DirtyField(ent, ent.Comp, nameof(InternalOrganComponent.OrganSeverity));

        var sevEv = new OrganDamageSeverityChanged(ent.Comp.OrganSeverity, nearestSeverity);
        RaiseLocalEvent(ent, ref sevEv);
        if (_container.TryGetContainingContainer(ent.Owner, out var container))
        {
            var ev1 = new OrganDamageSeverityChangedOnWoundable((ent, ent.Comp), ent.Comp.OrganSeverity, nearestSeverity);
            RaiseLocalEvent(container.Owner, ref ev1);
        }
    }

    #endregion
}
