// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Trauma.Common.RadialSelector;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.BloodCult.TimedFactory;

public sealed partial class TimedFactorySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<TimedFactoryComponent>();
        while (query.MoveNext(out var uid, out var factory))
        {
            _appearance.SetData(uid, GenericCultVisuals.State, now >= factory.NextProduce);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<TimedFactoryComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextProduce = _timing.CurTime + ent.Comp.Cooldown;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnUIOpenAttempt(Entity<TimedFactoryComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        var now = _timing.CurTime;
        if (now >= ent.Comp.NextProduce)
            return;

        var cooldown = Math.Ceiling((ent.Comp.NextProduce - now).TotalSeconds);
        if (!args.Silent)
            _popup.PopupEntity(Loc.GetString("timed-factory-cooldown", ("cooldown", cooldown)), ent, args.User);
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnUIOpen(Entity<TimedFactoryComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        _ui.SetUiState(ent.Owner, RadialSelectorUiKey.Key, new RadialSelectorState(ent.Comp.Entries));
    }

    [SubscribeLocalEvent]
    private void OnPrototypeSelected(Entity<TimedFactoryComponent> ent, ref RadialSelectorSelectedMessage args)
    {
        var now = _timing.CurTime;
        if (now < ent.Comp.NextProduce)
            return;

        var user = args.Actor;
        var product = PredictedSpawnAtPosition(args.SelectedItem, Transform(user).Coordinates);
        _hands.TryPickupAnyHand(user, product);
        ent.Comp.NextProduce = now + ent.Comp.Cooldown;
        Dirty(ent);
        _appearance.SetData(ent, GenericCultVisuals.State, false);
    }
}
