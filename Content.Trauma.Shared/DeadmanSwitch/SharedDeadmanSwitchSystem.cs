// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Examine;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.DeadmanSwitch;

/// <summary>
/// System for deadman's switch behavior.
/// Handles OnUseInHand event, preventing the signaller from being triggered the normal way.
/// Instead, using it in hand arms / disarms it, and it will then trigger if dropped while armed.
/// </summary>
public abstract partial class SharedDeadmanSwitchSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeadmanSwitchComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<DeadmanSwitchComponent, DeadmanSwitchDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DeadmanSwitchComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    /// Make the dead man's switch send out its remote signal.
    /// </summary>
    /// <param name="ent">The dead man's switch entity.</param>
    /// <param name="user">The entity responsible for triggering it, if applicable.</param>
    public virtual void Trigger(Entity<DeadmanSwitchComponent?> ent, EntityUid? user)
    {
    }

    private void ToggleArmed(Entity<DeadmanSwitchComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Armed = !ent.Comp.Armed;
        _appearance.SetData(ent, ToggleableVisuals.Enabled, ent.Comp.Armed);
        Dirty(ent);
    }

    private void OnDropped(Entity<DeadmanSwitchComponent> ent, ref DroppedEvent args)
    {
        if (!ent.Comp.Armed)
            return;

        ToggleArmed((ent.Owner, ent.Comp));
        Trigger((ent.Owner, ent.Comp), args.User);
    }

    protected void OnUseInHand(Entity<DeadmanSwitchComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.ArmDelay, new DeadmanSwitchDoAfterEvent(), ent, target: ent);
        _doAfter.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnDoAfter(Entity<DeadmanSwitchComponent> ent, ref DeadmanSwitchDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        ToggleArmed((ent.Owner, ent.Comp));
        ToggleInHandFeedback((ent.Owner, ent.Comp), args.User);
    }

    protected virtual void ToggleInHandFeedback(Entity<DeadmanSwitchComponent?> ent, EntityUid? user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (user != null)
            _popup.PopupPredicted(Loc.GetString(ent.Comp.Armed ? "deadman-on-activate" : "deadman-on-deactivate", ("name", ent)), ent, user);

        _audio.PlayPredicted(ent.Comp.SwitchSound, ent, user);
    }

    private void OnExamined(Entity<DeadmanSwitchComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.Armed ? "deadman-examine-armed" : "deadman-examine-disarmed"));
    }
}
