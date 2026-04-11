// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Station;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Antag;

public abstract class SharedAntagSummonerSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagSummonerComponent, MapInitEvent>(OnMapInit);
        Subs.BuiEvents<AntagSummonerComponent>(AntagSummonerUiKey.Key, subs =>
        {
            subs.Event<SummonAntagMessage>(OnSummonAntag);
        });
    }

    private void OnMapInit(Entity<AntagSummonerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextSummon = _timing.CurTime + ent.Comp.Cooldown;
        Dirty(ent);
    }

    private void OnSummonAntag(Entity<AntagSummonerComponent> ent, ref SummonAntagMessage args)
    {
        var user = args.Actor;
        if (!_access.IsAllowed(user, ent.Owner))
        {
            _popup.PopupClient("Access denied!", ent, user);
            return;
        }

        var now = _timing.CurTime;
        if (now < ent.Comp.NextSummon)
        {
            var minutes = (int) Math.Ceiling((ent.Comp.NextSummon - now).TotalMinutes);
            _popup.PopupClient($"Next security grant available in {minutes} minutes!", ent, user, PopupType.SmallCaution);
            return;
        }

        if (_station.GetOwningStation(ent.Owner) is not {} station)
        {
            _popup.PopupClient("You need to use it on a station!", ent, user, PopupType.SmallCaution);
            return;
        }

        ent.Comp.NextSummon = now + ent.Comp.Cooldown;
        Dirty(ent);

        _adminLog.Add(LogType.InteractHand, LogImpact.High, $"{ToPrettyString(user):user} summoned an antag with {ToPrettyString(ent):used}!");
        _popup.PopupClient("You pressed the red button...", ent, user, PopupType.LargeCaution);

        SummonAntag(ent);
        _cargo.TryAdjustBankAccount(station, ent.Comp.Account, ent.Comp.Reward);
    }

    protected virtual void SummonAntag(Entity<AntagSummonerComponent> ent)
    {
        // not predicted lol
    }
}
