// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Abilities.Mime;
using Content.Shared.Actions.Events;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.IdentityManagement;
using Content.Shared.Magic;
using Content.Shared.Popups;

namespace Content.Goobstation.Shared.Mimery;

public sealed partial class AdvancedMimerySystem : EntitySystem
{
    [Dependency] private SharedMagicSystem _magic = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, InvisibleBlockadeActionEvent>(OnInvisibleBlockade);

        SubscribeLocalEvent<AdvancedMimeryActionComponent, ActionAttemptEvent>(OnMimeryAttempt);
    }

    private void OnMimeryAttempt(Entity<AdvancedMimeryActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (!TryComp(args.User, out MimePowersComponent? powers))
            EnsureComp<MimePowersComponent>(args.User);
        else if (!powers.Enabled || powers.VowBroken)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.VowBrokenMessage), args.User, args.User);
            args.Cancelled = true;
        }
    }

    private void OnInvisibleBlockade(Entity<MimePowersComponent> ent, ref InvisibleBlockadeActionEvent args)
    {
        if (args.Handled || !ent.Comp.Enabled || ent.Comp.VowBroken)
            return;

        var transform = Transform(ent);
        foreach (var position in _magic.GetInstantSpawnPositions(transform, new TargetInFront()))
        {
            args.Handled = true;
            PredictedSpawnAttachedTo(ent.Comp.WallPrototype, position.SnapToGrid(EntityManager));
        }

        if (!args.Handled)
            return;

        var messageSelf = Loc.GetString("mime-invisible-wall-popup-self",
            ("mime", Identity.Entity(ent.Owner, EntityManager)));
        var messageOthers = Loc.GetString("mime-invisible-wall-popup-others",
            ("mime", Identity.Entity(ent.Owner, EntityManager)));
        _popup.PopupEntity(messageSelf, messageOthers, ent, ent);
    }
}
