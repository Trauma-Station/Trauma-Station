using Content.Shared.Abilities.Mime;
using Content.Shared.Actions.Events;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Magic;
using Content.Shared.Popups;
using Robust.Shared.Map;

namespace Content.Goobstation.Shared.Mimery;

public sealed class AdvancedMimerySystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMagicSystem _magic = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, InvisibleBlockadeActionEvent>(OnInvisibleBlockade);

        SubscribeLocalEvent<ActionRequiresEmptyHandComponent, ActionAttemptEvent>(OnHandAttempt);

        SubscribeLocalEvent<AdvancedMimeryActionComponent, ActionAttemptEvent>(OnMimeryAttempt);

        SubscribeLocalEvent<EntityEffectOnActionComponent, ActionPerformedEvent>(OnEffects);
    }

    private void OnEffects(Entity<EntityEffectOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        _effects.ApplyEffects(args.Performer, ent.Comp.Effects, ent.Comp.Scale, args.Performer);
    }

    private void OnMimeryAttempt(Entity<AdvancedMimeryActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (!TryComp(args.User, out MimePowersComponent? powers))
            EnsureComp<MimePowersComponent>(args.User);
        else if (!powers.Enabled || powers.VowBroken)
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.VowBrokenMessage), args.User, args.User);
            args.Cancelled = true;
        }
    }

    private void OnHandAttempt(Entity<ActionRequiresEmptyHandComponent> ent, ref ActionAttemptEvent args)
    {
        if (_hands.TryGetEmptyHand(args.User, out _))
            return;

        if (ent.Comp.PopupMessage is { } msg)
            _popup.PopupClient(Loc.GetString(msg), args.User, args.User);

        args.Cancelled = true;
    }

    private void OnInvisibleBlockade(Entity<MimePowersComponent> ent, ref InvisibleBlockadeActionEvent args)
    {
        if (args.Handled || !ent.Comp.Enabled || ent.Comp.VowBroken)
            return;

        var transform = Transform(ent);
        foreach (var position in _magic.GetInstantSpawnPositions(transform, new TargetInFront()))
        {
            args.Handled = true;
            PredictedSpawnAttachedTo(ent.Comp.WallPrototype, position.SnapToGrid(EntityManager, _mapMan));
        }

        if (!args.Handled)
            return;

        var messageSelf = Loc.GetString("mime-invisible-wall-popup-self",
            ("mime", Identity.Entity(ent.Owner, EntityManager)));
        var messageOthers = Loc.GetString("mime-invisible-wall-popup-others",
            ("mime", Identity.Entity(ent.Owner, EntityManager)));
        _popup.PopupPredicted(messageSelf, messageOthers, ent, ent);
    }
}
