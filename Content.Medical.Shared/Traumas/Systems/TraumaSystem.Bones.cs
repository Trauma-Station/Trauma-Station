// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.DoAfter;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Weapons;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Medical.Shared.Traumas;

public partial class TraumaSystem
{
    [Dependency] private EntityQuery<BoneComponent> _boneQuery = default!;
    [Dependency] private EntityQuery<OrganComponent> _organQuery = default!;

    #region Event Handling

    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<BoneComponent> ent, ref RejuvenateEvent args)
    {
        SetBoneIntegrity(ent.AsNullable(), ent.Comp.IntegrityCap);
    }

    [SubscribeLocalEvent]
    private void OnBoneSeverityChanged(Entity<BoneComponent> bone, ref BoneSeverityChangedEvent args)
    {
        if (args.NewSeverity < args.OldSeverity || // dgaf about healing
            !_organQuery.TryComp(bone, out var organ) ||
            organ.Category is not {} category ||
            organ.Body is not {} body)
            return;

        var partName = ProtoMan.Index(category).Name;
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

        // TODO SHITMED: predict bone damage!!?!
        _audio.PlayPvs(bone.Comp.BoneBreakSound, body, bone.Comp.BoneBreakSound.Params.WithVolume(volumeFloat));
    }

    [SubscribeLocalEvent]
    private void OnBoneIntegrityChanged(Entity<BoneComponent> bone, ref BoneIntegrityChangedEvent args)
    {
        if (args.NewIntegrity == bone.Comp.IntegrityCap)
            RemoveTraumas(bone.Owner, TraumaType.BoneDamage);
    }

    [SubscribeLocalEvent]
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

    public bool DamageBone(Entity<BoneComponent?> bone, FixedPoint2 severity)
    {
        if (severity == 0 || !_boneQuery.Resolve(bone, ref bone.Comp))
            return false;

        return SetBoneIntegrity(bone, bone.Comp.BoneIntegrity - severity);
    }

    public bool ApplyBoneTrauma(
        Entity<BoneComponent?> bone,
        Entity<TraumaInflicterComponent> wound,
        FixedPoint2 severity)
    {
        if (!_boneQuery.Resolve(bone, ref bone.Comp))
            return false;

        // TODO: predict when its rng is unfucked
        if (_net.IsServer)
            AddTrauma(bone, bone, wound, TraumaType.BoneDamage, severity);

        DamageBone(bone, severity);

        return true;
    }

    public bool SetBoneIntegrity(Entity<BoneComponent?> bone, FixedPoint2 integrity)
    {
        if (!_boneQuery.Resolve(bone, ref bone.Comp))
            return false;

        var newIntegrity = FixedPoint2.Clamp(integrity, 0, bone.Comp.IntegrityCap);
        if (bone.Comp.BoneIntegrity == newIntegrity)
            return false;

        var ev = new BoneIntegrityChangedEvent(bone.Comp.BoneIntegrity, newIntegrity);
        RaiseLocalEvent(bone, ref ev);

        bone.Comp.BoneIntegrity = newIntegrity;
        DirtyField(bone, bone.Comp, nameof(BoneComponent.BoneIntegrity));

        CheckBoneSeverity(bone);
        return true;
    }

    /// <summary>
    /// Updates the broken bones alert for a body based on its current bone state
    /// </summary>
    public void UpdateBodyBoneAlert(Entity<BodyComponent?> body)
    {
        bool hasBrokenBones = false;
        foreach (var bone in _body.GetOrgans<BoneComponent>(body))
        {
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

    #endregion

    #region Private API

    private void CheckBoneSeverity(Entity<BoneComponent?> bone)
    {
        if (!_boneQuery.Resolve(bone, ref bone.Comp))
            return;

        var nearestSeverity = bone.Comp.BoneSeverity;
        foreach (var (severity, value) in _boneThresholds.OrderByDescending(kv => kv.Value))
        {
            if (bone.Comp.BoneIntegrity < value)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity == bone.Comp.BoneSeverity)
            return;

        var ev = new BoneSeverityChangedEvent(bone.Comp.BoneSeverity, nearestSeverity);
        RaiseLocalEvent(bone, ref ev);

        bone.Comp.BoneSeverity = nearestSeverity;
        DirtyField(bone, nameof(BoneComponent.BoneSeverity));

        if (_body.GetBody(bone.Owner) is {} body)
            UpdateBodyBoneAlert(body);
    }

    #endregion
}
