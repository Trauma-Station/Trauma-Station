// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Animations;
using Content.Trauma.Shared.Wizard;
using Content.Trauma.Shared.Wizard.SupermatterHalberd;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Content.Trauma.Common.Wizard;

namespace Content.Trauma.Client.Wizard.Systems;

public sealed partial class SpellsSystem : SharedSpellsSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ActionTargetMarkSystem _mark = default!;
    [Dependency] private RaysSystem _rays = default!;

    public override event Action? StopTargeting;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WizardComponent, GetStatusIconsEvent>(GetWizardIcon);
        SubscribeLocalEvent<ApprenticeComponent, GetStatusIconsEvent>(GetApprenticeIcon);

        SubscribeNetworkEvent<StopTargetingEvent>(OnStopTargeting);
        SubscribeAllEvent<ChargeSpellRaysEffectEvent>(OnChargeEffect);
    }

    private void OnChargeEffect(ChargeSpellRaysEffectEvent ev)
    {
        var uid = GetEntity(ev.Uid);

        CreateChargeEffect(uid, ev);
    }

    protected override void CreateChargeEffect(EntityUid uid, ChargeSpellRaysEffectEvent ev)
    {
        if (!Timing.IsFirstTimePredicted || uid == EntityUid.Invalid)
            return;

        var rays = _rays.DoRays(TransformSystem.GetMapCoordinates(uid),
            Color.Yellow,
            Color.Fuchsia,
            10,
            15,
            minMaxRadius: new Vector2(3f, 6f),
            proto: "EffectRayCharge",
            server: false);

        if (rays == null)
            return;

        var track = EnsureComp<TrackUserComponent>(rays.Value);
        track.User = uid;
    }

    public override void SetSwapSecondaryTarget(EntityUid user, EntityUid? target, EntityUid action)
    {
        if (!TryComp<LockOnMarkActionComponent>(action, out var lockOn))
            return;

        var actionLockOn = (action, lockOn);

        if (target == null || user == target)
        {
            _mark.SetMark(actionLockOn, null);
            RaisePredictiveEvent(new SetSwapSecondaryTarget(GetNetEntity(action), null));
            return;
        }

        _mark.SetMark(actionLockOn, target);
        RaisePredictiveEvent(new SetSwapSecondaryTarget(GetNetEntity(action), GetNetEntity(target.Value)));
    }

    private void OnStopTargeting(StopTargetingEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession != _player.LocalSession)
            return;

        StopTargeting?.Invoke();
    }

    private void GetWizardIcon(Entity<WizardComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetApprenticeIcon(Entity<ApprenticeComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
