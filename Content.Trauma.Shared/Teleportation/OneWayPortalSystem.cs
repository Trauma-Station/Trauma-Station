// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Content.Shared.Teleportation.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Shared.Teleportation;

public sealed partial class OneWayPortalSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TeleportSystem _teleport = default!;

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
        if (ent.Owner == args.OurEntity)
            return;

        var subject = args.OtherEntity;

        if (_tag.HasTag(subject, PreventCollisionTag))
            return;

        // best not.
        if (Transform(subject).Anchored)
            return;

        // if they came from another portal, just return and wait for them to exit the portal
        if (HasComp<PortalTimeoutComponent>(subject))
            return;

        _teleport.Teleport(uid: subject, coords: ent.Comp.Destination, soundIn: ent.Comp.TeleportSound, soundOut: ent.Comp.TeleportSound);
    }
}
