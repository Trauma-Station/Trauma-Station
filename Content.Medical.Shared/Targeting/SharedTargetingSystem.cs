// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.Humanoid;

namespace Content.Medical.Shared.Targeting;

public abstract partial class SharedTargetingSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private EntityQuery<TargetingComponent> _query = default!;

    /// <summary>
    /// Array of all valid targeting enums.
    /// </summary>
    public static readonly TargetBodyPart[] ValidParts =
    [
        TargetBodyPart.Head,
        TargetBodyPart.Chest,
        TargetBodyPart.Groin,
        TargetBodyPart.LeftArm,
        TargetBodyPart.LeftHand,
        TargetBodyPart.LeftLeg,
        TargetBodyPart.LeftFoot,
        TargetBodyPart.RightArm,
        TargetBodyPart.RightHand,
        TargetBodyPart.RightLeg,
        TargetBodyPart.RightFoot,
    ];

    [SubscribeLocalEvent]
    private void OnGetTargetedPart(Entity<TargetingComponent> ent, ref GetTargetedPartEvent args)
    {
        if (args.Part != null)
            return;

        var (partType, symmetry) = _body.ConvertTargetBodyPart(ent.Comp.Target);
        args.Part = _part.FindBodyPart(args.Target, partType, symmetry)?.Owner;
    }

    [SubscribeLocalEvent, SubscribeNetworkEvent]
    private void OnChangeTarget(ChangeTargetMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        SetTarget(user, msg.BodyPart);
    }

    public void SetTarget(EntityUid user, TargetBodyPart target)
    {
        if (!_query.TryComp(user, out var comp) || comp.Target == target)
            return;

        comp.Target = target;
        Dirty(user, comp);
    }
}
