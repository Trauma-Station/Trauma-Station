// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Tag;
using Content.Shared.Teleportation.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Trauma.Shared.Teleportation;

public sealed partial class OneWayPortalSystem : EntitySystem
{
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TeleportSystem _teleport = default!;
    [Dependency] private SharedJointSystem _joints = default!;

    private static readonly ProtoId<TagPrototype> ShowTraverseVerbTag = "AllowPortalTraversal";
    private static readonly ProtoId<TagPrototype> PreventCollisionTag = "PreventPortalCollision";

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<OneWayPortalComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        // Traversal altverb for ghosts to use that bypasses normal functionality
        if (!args.CanAccess || !_tag.HasTag(args.User, ShowTraverseVerbTag))
            return;

        var subject = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 11,
            Act = () =>
            {
                _teleport.Teleport(subject, ent.Comp.Destination);
            },
            Disabled = false,
            Text = Loc.GetString("portal-component-ghost-traverse"),
            Message = Loc.GetString("portal-component-ghost-traverse"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"))
        });
    }

    [SubscribeLocalEvent]
    private void OnCollide(Entity<OneWayPortalComponent> ent, ref StartCollideEvent args)
    {
        var subject = args.OtherEntity;

        if (_tag.HasTag(args.OtherEntity, PreventCollisionTag))
            return;

        // best not.
        if (Transform(subject).Anchored)
            return;

        // break pulls before portal enter so we don't break shit
        if (TryComp<PullableComponent>(subject, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(subject, pullable, ignoreGrab: true);
        }

        if (TryComp<PullerComponent>(subject, out var pullerComp)
            && TryComp<PullableComponent>(pullerComp.Pulling, out var subjectPulling))
        {
            _pulling.TryStopPull(pullerComp.Pulling.Value, subjectPulling, ignoreGrab: true);
        }

        // also break grapple joints
        _joints.RemoveJoint(subject, SharedGrapplingGunSystem.GrapplingJoint);

        // if they came from another portal, just return and wait for them to exit the portal
        if (HasComp<PortalTimeoutComponent>(subject))
        {
            return;
        }

        _teleport.Teleport(uid: subject, coords: ent.Comp.Destination, soundIn: ent.Comp.TeleportSound, soundOut: ent.Comp.TeleportSound);
    }
}
