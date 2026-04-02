// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.BlockTeleport;
using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Bible;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;

public sealed class CosmicRunesSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStarMarkSystem _starMark = default!;
    [Dependency] private readonly TouchSpellSystem _touchSpell = default!;

    private HashSet<Entity<StarMarkComponent>> _teleporting = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCosmicRuneComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticCosmicRuneComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<HereticCosmicRuneComponent, AfterInteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<HereticCosmicRuneComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (HasComp<FadingTimedDespawnComponent>(ent))
            return;

        if (HasComp<StarTouchComponent>(args.Used))
        {
            _touchSpell.InvokeTouchSpell(args.Used, args.User);
            EnsureComp<FadingTimedDespawnComponent>(ent).Lifetime = 0f;
            if (Exists(ent.Comp.LinkedRune))
                EnsureComp<FadingTimedDespawnComponent>(ent.Comp.LinkedRune.Value).Lifetime = 0f;
            args.Handled = true;
            return;
        }

        if (!TryComp(args.Used, out BibleComponent? bible) ||
            !HasComp<BibleUserComponent>(args.User) || !TryComp(args.Used, out UseDelayComponent? useDelay) ||
            _useDelay.IsDelayed((args.Used, useDelay)))
            return;

        _useDelay.TryResetDelay(args.Used, false, useDelay);
        _audio.PlayPredicted(bible.HealSoundPath, Transform(ent).Coordinates, args.User);
        EnsureComp<FadingTimedDespawnComponent>(ent).Lifetime = 0f;
        args.Handled = true;
    }

    private void OnActivate(Entity<HereticCosmicRuneComponent> ent, ref ActivateInWorldEvent args)
    {
        if (Teleport(ent, args.User))
            args.Handled = true;
    }

    private void OnInteract(Entity<HereticCosmicRuneComponent> ent, ref InteractHandEvent args)
    {
        if (Teleport(ent, args.User))
            args.Handled = true;
    }

    private bool Teleport(Entity<HereticCosmicRuneComponent> ent, EntityUid user)
    {
        var time = _timing.CurTime;

        if (time < ent.Comp.NextUse)
            return false;

        if (HasComp<FadingTimedDespawnComponent>(ent))
            return false;

        if (!Exists(ent.Comp.LinkedRune) || !TryComp(ent.Comp.LinkedRune.Value, out TransformComponent? xform) ||
            !xform.Coordinates.IsValid(EntityManager) ||
            HasComp<FadingTimedDespawnComponent>(ent.Comp.LinkedRune.Value))
        {
            if (_net.IsServer) // Client can have rune deleted due to PVS but can exist on server
                _popup.PopupEntity(Loc.GetString("heretic-cosmic-rune-fail-unlinked"), user, user);
            return false;
        }

        if (HasComp<StarMarkComponent>(user))
        {
            _popup.PopupClient(Loc.GetString("heretic-cosmic-rune-fail-star-mark"), user, user);
            return false;
        }

        if (!_transform.InRange(ent.Owner, user, ent.Comp.Range))
        {
            _popup.PopupClient(Loc.GetString("heretic-cosmic-rune-fail-range"), user, user);
            return false;
        }

        var ev = new TeleportAttemptEvent();
        RaiseLocalEvent(user, ref ev);
        if (ev.Cancelled)
            return false;

        ent.Comp.NextUse = time + ent.Comp.Delay;
        DirtyField(ent.Owner, ent.Comp, nameof(HereticCosmicRuneComponent.NextUse));
        if (TryComp(ent.Comp.LinkedRune.Value, out HereticCosmicRuneComponent? rune2))
        {
            rune2.NextUse = time + rune2.Delay;
            DirtyField(ent.Comp.LinkedRune.Value, rune2, nameof(HereticCosmicRuneComponent.NextUse));
        }

        if (_net.IsServer)
        {
            _audio.PlayPvs(ent.Comp.Sound, ent);
            _audio.PlayPvs(ent.Comp.Sound, ent.Comp.LinkedRune.Value);
            SpawnAttachedTo(ent.Comp.Effect, ent.Owner.ToCoordinates());
            SpawnAttachedTo(ent.Comp.Effect, ent.Comp.LinkedRune.Value.ToCoordinates());
        }

        var coords = Transform(ent).Coordinates;
        _teleporting.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Range, _teleporting, LookupFlags.Dynamic);
        _teleporting.Add((user, default!)); // also teleport the user
        EntityUid? pulling = null;
        var grabStage = GrabStage.No;
        PullerComponent? puller = null;

        var isUserCosmosHeretic = HasComp<StarGazerComponent>(user) || HasComp<CosmosPassiveComponent>(user);

        if (isUserCosmosHeretic && TryComp(user, out puller) && puller.Pulling is { } pulled)
        {
            pulling = pulled;
            grabStage = puller.GrabStage;
            _teleporting.Add((pulled, default!));
        }

        foreach (var entity in _teleporting)
        {
            var uid = entity.Owner;
            _pulling.StopAllPulls(uid);
            // TODO: replace with teleport system
            _transform.SetCoordinates(uid, xform.Coordinates);
            _starMark.TryApplyStarMark(uid);
        }

        if (pulling != null)
            _pulling.TryStartPull(user, pulling.Value, puller, null, grabStage, force: true);

        return true;
    }
}
