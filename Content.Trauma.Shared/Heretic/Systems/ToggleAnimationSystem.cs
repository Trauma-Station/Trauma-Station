// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using Content.Shared.Item.ItemToggle.Components;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed class ToggleAnimationSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleAnimationComponent, ItemToggledEvent>(OnToggle);
        SubscribeLocalEvent<ToggleAnimationComponent, ComponentStartup>(OnStartup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<ToggleAnimationComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var toggle, out var appearance))
        {
            if (toggle.NextState == toggle.CurState)
                continue;

            if (toggle.ToggleEndTime > now)
                continue;

            toggle.CurState = toggle.NextState;
            _appearance.SetData(uid, ToggleAnimationVisuals.ToggleState, toggle.NextState, appearance);
        }
    }

    private void OnStartup(Entity<ToggleAnimationComponent> ent, ref ComponentStartup args)
    {
        var state = TryComp(ent, out ItemToggleComponent? toggle) && toggle.Activated
            ? ToggleAnimationState.On
            : ToggleAnimationState.Off;

        _appearance.SetData(ent, ToggleAnimationVisuals.ToggleState, state);
        ent.Comp.CurState = state;
        ent.Comp.NextState = state;
    }

    private void OnToggle(Entity<ToggleAnimationComponent> ent, ref ItemToggledEvent args)
    {
        if (_net.IsClient)
            return;

        var (uid, comp) = ent;

        var (state, timer, nextState) = args.Activated
            ? (ToggleAnimationState.TogglingOn, comp.ToggleOnTime, ToggleAnimationState.On)
            : (ToggleAnimationState.TogglingOff, comp.ToggleOffTime, ToggleAnimationState.Off);

        _appearance.SetData(uid, ToggleAnimationVisuals.ToggleState, state);
        ent.Comp.NextState = nextState;
        ent.Comp.ToggleEndTime = _timing.CurTime + timer;
    }
}
