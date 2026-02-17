// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Tools;

public sealed class EffectsToolSystem : EntitySystem
{
    [Dependency] private readonly EffectDataSystem _data = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<EffectsToolComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<EffectsToolComponent>();

        SubscribeLocalEvent<EffectsToolComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<EffectsToolComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);
        SubscribeLocalEvent<EffectsToolComponent, EffectsToolDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<EffectsToolComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not {} target)
            return;

        var user = args.User;
        if (!ValidTarget(ent, target, user, popup: true))
        {
            // if it made a popup then don't allow other interactions
            args.Handled = ent.Comp.InvalidPopup != null;
            return;
        }

        args.Handled = true;
        StartDoAfter(ent, target, user);
    }

    private void OnGetVerbs(Entity<EffectsToolComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var target = args.Target;
        var user = args.User;
        // no popup for the verb so right clicking doesn't randomly popup
        if (!ValidTarget(ent, target, user))
            return;

        args.Verbs.Add(new UtilityVerb()
        {
            Act = () => StartDoAfter(ent, target, user),
            Text = Loc.GetString(ent.Comp.VerbText),
            Icon = ent.Comp.VerbIcon
        });
    }

    private void OnDoAfter(Entity<EffectsToolComponent> ent, ref EffectsToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not {} target)
            return;

        args.Handled = true;
        UseTool(ent, target, args.User);
    }

    public void StartDoAfter(Entity<EffectsToolComponent> ent, EntityUid target, EntityUid user)
    {
        if (!CanUse(ent, target, user))
            return;

        var args = new DoAfterArgs(EntityManager,
            user,
            ent.Comp.Delay,
            new EffectsToolDoAfterEvent(),
            eventTarget: ent.Owner,
            target: target,
            used: ent);
        _doAfter.TryStartDoAfter(args);
    }

    public bool CanUse(Entity<EffectsToolComponent> ent, EntityUid target, EntityUid user)
    {
        if (!ValidTarget(ent, target, user)) // don't need popup as it should have already been checked before calling StartDoAfter
            return false;

        var ev = new EffectsToolUseAttemptEvent(target, user);
        RaiseLocalEvent(ent, ref ev);
        return !ev.Cancelled;
    }

    public bool ValidTarget(Entity<EffectsToolComponent> ent, EntityUid target, EntityUid user, bool popup = false)
    {
        if (_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return true;

        if (popup && ent.Comp.InvalidPopup is {} key)
        {
            var msg = Loc.GetString(key, ("target", Identity.Name(target, EntityManager)));
            _popup.PopupClient(msg, ent, user);
        }
        return false;
    }

    public bool UseTool(Entity<EffectsToolComponent> ent, EntityUid target, EntityUid user)
    {
        if (!CanUse(ent, target, user))
            return false;

        // do the thing, effects are expected to call MarkUsed
        ent.Comp.Used = false;
        _data.SetTool(target, ent);
        _effects.ApplyEffects(target, ent.Comp.Effects, user: user);
        _data.ClearTool(target);

        if (!ent.Comp.Used)
            return false;

        // use resources etc
        var ev = new EffectsToolUsedEvent(target, user);
        RaiseLocalEvent(ent, ref ev);

        // feedback
        var targetName = Identity.Name(target, EntityManager);
        var userName = Identity.Name(user, EntityManager);
        var you = Loc.GetString(ent.Comp.UserPopup, ("used", ent), ("target", targetName));
        var others = Loc.GetString(ent.Comp.OthersPopup, ("used", ent), ("target", targetName), ("user", userName));
        _popup.PopupPredicted(
            you,
            others,
            target,
            user);
        _audio.PlayPredicted(ent.Comp.Sound, target, user);
        return true;
    }

    /// <summary>
    /// Called by an effect to mark that the tool was used.
    /// </summary>
    /// <remarks>
    /// Only needed because "ECS" entity effects are cruel and don't let you handle the args anymore.
    /// TheShuEd my goat.
    /// </remarks>
    public void MarkUsed(Entity<EffectsToolComponent?> ent)
    {
        if (_query.Resolve(ent, ref ent.Comp))
            ent.Comp.Used = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class EffectsToolDoAfterEvent : SimpleDoAfterEvent;
