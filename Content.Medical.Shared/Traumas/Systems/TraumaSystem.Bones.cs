// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.DoAfter;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Weapons;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Medical.Shared.Traumas;

public partial class TraumaSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private void InitBones()
    {
        SubscribeLocalEvent<BoneComponent, BoneSeverityChangedEvent>(OnBoneSeverityChanged);
        SubscribeLocalEvent<BoneComponent, BoneIntegrityChangedEvent>(OnBoneIntegrityChanged);
        SubscribeLocalEvent<BoneComponent, ModifyDoAfterDelayEvent>(OnModifyDoAfterDelay);
    }

    #region Event Handling

    private void OnBoneSeverityChanged(Entity<BoneComponent> bone, ref BoneSeverityChangedEvent args)
    {
        if (bone.Comp.BoneWoundable is not {} part ||
            args.NewSeverity < args.OldSeverity ||
            !TryComp<OrganComponent>(part, out var organ) ||
            organ.Category is not {} category ||
            organ.Body is not {} body)
            return;

        // TODO SHITMED: predict bone damage!!?!
        var partName = _proto.Index(category).Name;
        _popup.PopupEntity(Loc.GetString($"popup-trauma-BoneDamage-{args.NewSeverity}", ("part", partName)),
            body,
            body,
            PopupType.SmallCaution);

        var volumeFloat = args.NewSeverity switch
        {
            BoneSeverity.Damaged => -8f,
            BoneSeverity.Cracked => 1f,
            BoneSeverity.Broken => 6f,
            _ => 0f,
        };

        _audio.PlayPvs(bone.Comp.BoneBreakSound, body, AudioParams.Default.WithVolume(volumeFloat));
    }

    private void OnBoneIntegrityChanged(Entity<BoneComponent> bone, ref BoneIntegrityChangedEvent args)
    {
        if (bone.Comp.BoneWoundable is not {} part || _body.GetBody(part) is not {} body)
            return;

        if (args.NewIntegrity == bone.Comp.IntegrityCap)
        {
            if (TryGetWoundableTrauma(bone.Comp.BoneWoundable.Value, out var traumas, TraumaType.BoneDamage))
            {
                foreach (var trauma in traumas)
                {
                    if (trauma.Comp.TraumaTarget == bone)
                        RemoveTrauma(trauma);
                }
            }
        }

        var ev = new PartBoneDamageChangedEvent(bone, body, args.NewIntegrity);
        RaiseLocalEvent(part, ref ev);
    }

    private void OnModifyDoAfterDelay(Entity<BoneComponent> bone, ref ModifyDoAfterDelayEvent args)
    {
        args.Multiplier /= bone.Comp.BoneSeverity switch
        {
            BoneSeverity.Damaged => 0.92f,
            BoneSeverity.Cracked => 0.84f,
            BoneSeverity.Broken => 0.75f,
            _ => 1f,
        };
    }

    #endregion

    #region Public API

    public bool ApplyDamageToBone(EntityUid bone, FixedPoint2 severity, BoneComponent? boneComp = null)
    {
        if (severity == 0
            || !Resolve(bone, ref boneComp))
            return false;

        var newIntegrity = FixedPoint2.Clamp(boneComp.BoneIntegrity - severity, 0, boneComp.IntegrityCap);
        if (boneComp.BoneIntegrity == newIntegrity)
            return false;

        var ev = new BoneIntegrityChangedEvent((bone, boneComp), boneComp.BoneIntegrity, newIntegrity);
        RaiseLocalEvent(bone, ref ev);

        boneComp.BoneIntegrity = newIntegrity;
        CheckBoneSeverity(bone, boneComp);

        Dirty(bone, boneComp);
        return true;
    }

    public bool ApplyBoneTrauma(
        EntityUid boneEnt,
        Entity<WoundableComponent> woundable,
        Entity<TraumaInflicterComponent> inflicter,
        FixedPoint2 inflicterSeverity,
        BoneComponent? boneComp = null)
    {
        if (!Resolve(boneEnt, ref boneComp))
            return false;

        if (_net.IsServer)
            AddTrauma(boneEnt, woundable, inflicter, TraumaType.BoneDamage, inflicterSeverity);

        ApplyDamageToBone(boneEnt, inflicterSeverity, boneComp);

        return true;
    }

    public bool SetBoneIntegrity(EntityUid bone, FixedPoint2 integrity, BoneComponent? boneComp = null)
    {
        if (!Resolve(bone, ref boneComp))
            return false;

        var newIntegrity = FixedPoint2.Clamp(integrity, 0, boneComp.IntegrityCap);
        if (boneComp.BoneIntegrity == newIntegrity)
            return false;

        var ev = new BoneIntegrityChangedEvent((bone, boneComp), boneComp.BoneIntegrity, newIntegrity);
        RaiseLocalEvent(bone, ref ev);

        boneComp.BoneIntegrity = newIntegrity;
        CheckBoneSeverity(bone, boneComp);

        Dirty(bone, boneComp);
        return true;
    }

    /// <summary>
    /// Updates the broken bones alert for a body based on its current bone state
    /// </summary>
    public void UpdateBodyBoneAlert(Entity<BodyComponent?> body)
    {
        if (!Resolve(body, ref body.Comp))
            return;

        bool hasBrokenBones = false;
        foreach (var woundable in _body.GetOrgans<WoundableComponent>(body))
        {
            if (GetBone(woundable.AsNullable()) is not {} bone)
                continue;

            if (bone.Comp.BoneSeverity == BoneSeverity.Broken)
            {
                hasBrokenBones = true;
                break;
            }
        }

        // Update the alert based on whether any bones are broken
        if (hasBrokenBones)
            _alert.ShowAlert(body.Owner, _brokenBonesAlertId);
        else
            _alert.ClearAlert(body.Owner, _brokenBonesAlertId);
    }

    public Entity<BoneComponent>? GetBone(Entity<WoundableComponent?> ent)
        => Resolve(ent, ref ent.Comp) &&
            ent.Comp.Bone.ContainedEntities.FirstOrNull() is {} bone &&
            TryComp<BoneComponent>(bone, out var boneComp)
            ? (bone, boneComp)
            : null;

    #endregion

    #region Private API

    private void CheckBoneSeverity(EntityUid bone, BoneComponent boneComp)
    {
        var nearestSeverity = boneComp.BoneSeverity;

        foreach (var (severity, value) in _boneThresholds.OrderByDescending(kv => kv.Value))
        {
            if (boneComp.BoneIntegrity < value)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity != boneComp.BoneSeverity)
        {
            var ev = new BoneSeverityChangedEvent((bone, boneComp), boneComp.BoneSeverity, nearestSeverity);
            RaiseLocalEvent(bone, ref ev, true);
        }

        boneComp.BoneSeverity = nearestSeverity;
        Dirty(bone, boneComp);

        if (boneComp.BoneWoundable is {} part && _body.GetBody(part) is {} body)
            UpdateBodyBoneAlert(body);
    }

    #endregion
}
