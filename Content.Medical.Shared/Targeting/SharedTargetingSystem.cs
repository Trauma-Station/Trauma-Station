// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.Humanoid;

namespace Content.Medical.Shared.Targeting;

public abstract class SharedTargetingSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;

    private EntityQuery<TargetingComponent> _query;

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

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<TargetingComponent>();

        SubscribeLocalEvent<TargetingComponent, GetTargetedPartEvent>(OnGetTargetedPart);
        SubscribeAllEvent<ChangeTargetMessage>(OnChangeTarget);
    }

    private void OnGetTargetedPart(Entity<TargetingComponent> ent, ref GetTargetedPartEvent args)
    {
        if (args.Part != null)
            return;

        var (partType, symmetry) = _body.ConvertTargetBodyPart(ent.Comp.Target);
        args.Part = _part.FindBodyPart(args.Target, partType, symmetry)?.Owner;
    }

    private void OnChangeTarget(ChangeTargetMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user ||
            !_query.TryComp(user, out var comp) ||
            comp.Target == msg.BodyPart)
            return;

        comp.Target = msg.BodyPart;
        Dirty(user, comp);
    }
}
