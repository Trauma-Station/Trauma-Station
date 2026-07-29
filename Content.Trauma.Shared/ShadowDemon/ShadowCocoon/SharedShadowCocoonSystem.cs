// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Sound.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ShadowDemon.ShadowCocoon;

public abstract partial class SharedShadowCocoonSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<ShadowCocoonMakerComponent> _makerQuery = default!;

    #region Shadow Cocoon Maker
    [SubscribeLocalEvent]
    private void OnGetAltVerbs(Entity<CanBeShadowCocoonComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!_makerQuery.TryComp(args.User, out var maker))
            return;

        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        var target = args.Target;
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = maker.VerbText,
            Act = () =>
            {
                StartCocooning(user, target, maker.CocoonDelay);
            }
        });
    }

    [SubscribeLocalEvent]
    private void OnCocoonDoAfter(Entity<CanBeShadowCocoonComponent> ent, ref ShadowCocoonDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Target is not {} target ||
            HasComp<InsideEntityStorageComponent>(target) || // no infinite eggs
            !_makerQuery.TryComp(args.User, out var maker))
            return;

        var spawnAt = Transform(target).Coordinates;
        var cocoon = PredictedSpawnAtPosition(maker.ShadowCocoon, spawnAt);

        _entityStorage.Insert(target, cocoon);

        _adminLog.Add(LogType.Verb, LogImpact.High,
            $"{args.User} spawned a shadow cocoon and put {target} inside");
    }

    private void StartCocooning(EntityUid user, EntityUid target, TimeSpan delay)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            delay,
            new ShadowCocoonDoAfterEvent(),
            target,
            target)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }
    #endregion

    #region Shadow Cocoon Entity
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ShadowCocoonComponent> ent, ref MapInitEvent args) =>
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateDelay;

    /// <summary>
    /// Shadow Cocoon has its own alternative verbs, which give it the ability to make random sounds via RandomIntervalSoundComponent when activated.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnGetAltShadowCocoonVerbs(Entity<ShadowCocoonComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!_makerQuery.HasComp(args.User))
            return;

        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("shadow-cocoon-activate-sounds-verb"),
            Act = () =>
            {
                ent.Comp.Silent = !ent.Comp.Silent;
                Dirty(ent);

                if (!ent.Comp.Silent)
                {
                    var sounds = EnsureComp<RandomIntervalSoundComponent>(ent.Owner);
                    sounds.Sound = ent.Comp.RandomSounds;

                    _popup.PopupEntity(Loc.GetString("shadow-cocoon-halluc-activated"), user, user);
                    return;
                }

                _popup.PopupEntity(Loc.GetString("shadow-cocoon-halluc-deactivated"), user, user);
                RemCompDeferred<RandomIntervalSoundComponent>(ent.Owner);
            }
        });
    }
    #endregion
}
