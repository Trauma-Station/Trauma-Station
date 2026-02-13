// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Components;

namespace Content.Medical.Shared.Traumas;

/// <summary>
/// Implements gameplay effects when you have broken bones.
/// </summary>
// TODO: make this 1000x better this is such a nothingburger
public sealed class BoneEffectsSystem : EntitySystem
{
    [Dependency] private readonly SharedVirtualItemSystem _virtual = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandOrganComponent, PartBoneDamageChangedEvent>(OnHandDamageChanged);
        SubscribeLocalEvent<MovementBodyPartComponent, PartBoneDamageChangedEvent>(OnLegDamageChanged);
    }

    private void OnHandDamageChanged(Entity<HandOrganComponent> ent, ref PartBoneDamageChangedEvent args)
    {
        if (args.NewIntegrity == args.Bone.Comp.IntegrityCap)
            _virtual.DeleteInHandsMatching(args.Body, args.Bone);
    }

    private void OnLegDamageChanged(Entity<MovementBodyPartComponent> ent, ref PartBoneDamageChangedEvent args)
    {
        // TODO NUBODY: broken legs give you a STATUS EFFECT that modifies speed, not a fucking 65 line slopfunction
    }

    /* TODO NUBODY: kys
    private void ProcessLegsState(Entity<LegsComponent?> body)
    {
        if (!Resolve(body, ref body.Comp))
            return;

        var rawWalkSpeed = 0f; // just used to compare to actual speed values
        var walkSpeed = 0f;
        var sprintSpeed = 0f;
        var acceleration = 0f;

        foreach (var legEntity in body.Comp.Legs)
        {
            if (!TryComp<MovementBodyPartComponent>(legEntity, out var movement))
                continue;

            var partWalkSpeed = movement.WalkSpeed;
            var partSprintSpeed = movement.SprintSpeed;
            var partAcceleration = movement.Acceleration;

            if (!TryComp<WoundableComponent>(legEntity, out var legWoundable))
                continue;

            if (!TryComp<BoneComponent>(legWoundable.Bone.ContainedEntities.First(), out var boneComp))
                continue;

            // Get the foot penalty
            var penalty = 1f;
            var footEnt =
                _body.GetBodyChildrenOfType(body,
                        BodyPartType.Foot,
                        symmetry: Comp<BodyPartComponent>(legEntity).Symmetry)
                    .FirstOrNull();

            if (footEnt != null)
            {
                if (TryComp<BoneComponent>(legWoundable.Bone.ContainedEntities.FirstOrNull(), out var footBone))
                {
                    penalty = footBone.BoneSeverity switch
                    {
                        BoneSeverity.Damaged => 0.77f,
                        BoneSeverity.Cracked => 0.66f,
                        BoneSeverity.Broken => 0.55f,
                        _ => penalty,
                    };
                }
            }
            else
            {
                // You are supposed to have one
                penalty = 0.44f;
            }

            rawWalkSpeed += partWalkSpeed;
            partWalkSpeed *= penalty;
            partSprintSpeed *= penalty;
            partAcceleration *= penalty;

            switch (boneComp.BoneSeverity)
            {
                case BoneSeverity.Cracked:
                    walkSpeed += partWalkSpeed / 2f;
                    sprintSpeed += partSprintSpeed / 2f;
                    acceleration += partAcceleration / 2f;
                    break;

                case BoneSeverity.Damaged:
                    walkSpeed += partWalkSpeed / 1.6f;
                    sprintSpeed += partSprintSpeed / 1.6f;
                    acceleration += partAcceleration / 1.6f;
                    break;

                case BoneSeverity.Normal:
                    walkSpeed += partWalkSpeed;
                    sprintSpeed += partSprintSpeed;
                    acceleration += partAcceleration;
                    break;
            }
        }

        rawWalkSpeed /= bodyComp.RequiredLegs;
        walkSpeed /= bodyComp.RequiredLegs;
        sprintSpeed /= bodyComp.RequiredLegs;
        acceleration /= bodyComp.RequiredLegs;

        _movementSpeed.ChangeBaseSpeed(body, walkSpeed, sprintSpeed, acceleration);

        if (walkSpeed < rawWalkSpeed / 3.4)
            _standing.Down(body);
    }*/
}
