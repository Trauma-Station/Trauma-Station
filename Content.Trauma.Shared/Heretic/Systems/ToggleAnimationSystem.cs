// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle.Components;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed partial class ToggleAnimationSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleAnimationComponent, ItemToggledEvent>(OnToggle);
        SubscribeLocalEvent<ToggleAnimationComponent, MapInitEvent>(OnMapInit);
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
            Dirty(uid, toggle);
        }
    }

    private void OnMapInit(Entity<ToggleAnimationComponent> ent, ref MapInitEvent args)
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

        var now = _timing.CurTime;

        switch (args.Activated)
        {
            case true when comp.CurState == ToggleAnimationState.TogglingOn:
                return;
            case true when comp.CurState == ToggleAnimationState.TogglingOff:
                if (comp.ContinueReverseAnimation &&
                    comp.ToggleOffTime > TimeSpan.Zero && comp.ToggleOnTime > TimeSpan.Zero)
                {
                    _appearance.SetData(uid, ToggleAnimationVisuals.ToggleState, ToggleAnimationState.TogglingOn);
                    comp.CurState = ToggleAnimationState.TogglingOn;
                    comp.NextState = ToggleAnimationState.On;

                    var factor = (float) (comp.ToggleOffTime.TotalSeconds / comp.ToggleOnTime.TotalSeconds);
                    var progress = InverseLerp(comp.ToggleStartTime.TotalSeconds,
                        comp.ToggleEndTime.TotalSeconds,
                        now.TotalSeconds);
                    var invProgress = 1f - progress;

                    comp.ToggleStartTime = now - MathF.Pow(invProgress, factor) * comp.ToggleOnTime;
                    comp.ToggleEndTime = now + MathF.Pow(progress, factor) * comp.ToggleOnTime;
                    Dirty(ent);
                    return;
                }
                _appearance.SetData(uid, ToggleAnimationVisuals.ToggleState, ToggleAnimationState.On);
                comp.CurState = ToggleAnimationState.On;
                comp.NextState = ToggleAnimationState.On;
                comp.ToggleStartTime = TimeSpan.Zero;
                comp.ToggleEndTime = TimeSpan.Zero;
                Dirty(ent);
                return;
            case false when comp.CurState == ToggleAnimationState.TogglingOff:
                return;
            case false when comp.CurState == ToggleAnimationState.TogglingOn:
                if (comp.ContinueReverseAnimation &&
                    comp.ToggleOffTime > TimeSpan.Zero && comp.ToggleOnTime > TimeSpan.Zero)
                {
                    _appearance.SetData(uid, ToggleAnimationVisuals.ToggleState, ToggleAnimationState.TogglingOff);

                    comp.CurState = ToggleAnimationState.TogglingOff;
                    comp.NextState = ToggleAnimationState.Off;

                    var factor = (float) (comp.ToggleOnTime.TotalSeconds / comp.ToggleOffTime.TotalSeconds);
                    var progress = InverseLerp(comp.ToggleStartTime.TotalSeconds,
                        comp.ToggleEndTime.TotalSeconds,
                        now.TotalSeconds);
                    var invProgress = 1f - progress;

                    comp.ToggleStartTime = now - MathF.Pow(invProgress, factor) * comp.ToggleOffTime;
                    comp.ToggleEndTime = now + MathF.Pow(progress, factor) * comp.ToggleOffTime;
                    Dirty(ent);
                    return;
                }
                _appearance.SetData(uid, ToggleAnimationVisuals.ToggleState, ToggleAnimationState.Off);
                comp.CurState = ToggleAnimationState.Off;
                comp.NextState = ToggleAnimationState.Off;
                comp.ToggleStartTime = TimeSpan.Zero;
                comp.ToggleEndTime = TimeSpan.Zero;
                Dirty(ent);
                return;
        }

        var (state, timer, nextState) = args.Activated
            ? (ToggleAnimationState.TogglingOn, comp.ToggleOnTime, ToggleAnimationState.On)
            : (ToggleAnimationState.TogglingOff, comp.ToggleOffTime, ToggleAnimationState.Off);

        _appearance.SetData(uid, ToggleAnimationVisuals.ToggleState, state);
        ent.Comp.CurState = state;
        ent.Comp.NextState = nextState;
        ent.Comp.ToggleStartTime = now;
        ent.Comp.ToggleEndTime = now + timer;
        Dirty(ent);
    }

    private float InverseLerp(double min, double max, double value)
    {
        return max <= min ? 1f : (float) Math.Clamp((value - min) / (max - min), 0f, 1f);
    }
}
