// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Speech;
using Content.Goobstation.Shared.Loudspeaker.Components;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Trauma.Common.Speech;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Shared.Loudspeaker.Systems;

public sealed partial class LoudSpeakerSystem : EntitySystem
{

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {

        base.Initialize();

        SubscribeLocalEvent<LoudspeakerComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<LoudspeakerComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<LoudspeakerComponent, GotEquippedHandEvent>(OnEquippedHands);
        SubscribeLocalEvent<LoudspeakerComponent, GotUnequippedHandEvent>(OnUnequippedHands);

        SubscribeLocalEvent<LoudspeakerHolderComponent, SpeechFontSizeOverrideEvent>(OnGetLoudspeakerHolder);
        SubscribeLocalEvent<LoudspeakerComponent, SpeechFontSizeOverrideEvent>(OnGetLoudspeakerData);
        SubscribeLocalEvent<LoudspeakerHolderComponent, GetSpeechSoundEvent>(OnGetSpeechSound);

        SubscribeLocalEvent<LoudspeakerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<LoudspeakerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

    }

    private void OnEquipped(EntityUid uid, LoudspeakerComponent comp, GotEquippedEvent args)
    {
        if (!args.SlotFlags.HasFlag(comp.RequiredSlot))
            return;

        EnsureComp<LoudspeakerHolderComponent>(args.EquipTarget).Loudspeakers.Add(uid);
    }

    private void OnUnequipped(EntityUid uid, LoudspeakerComponent comp, GotUnequippedEvent args)
    {
        if (!TryComp<LoudspeakerHolderComponent>(args.EquipTarget, out var holder))
            return;

        holder.Loudspeakers.Remove(uid);

        DoRemovalCheck(args.EquipTarget, holder);
    }

    private void OnEquippedHands(EntityUid uid, LoudspeakerComponent comp, GotEquippedHandEvent args)
    {
        if (!comp.WorksInHand)
            return;

        EnsureComp<LoudspeakerHolderComponent>(args.User).Loudspeakers.Add(uid);
    }

    private void OnUnequippedHands(EntityUid uid, LoudspeakerComponent comp, GotUnequippedHandEvent args)
    {
        if (!TryComp<LoudspeakerHolderComponent>(args.User, out var holder))
            return;

        holder.Loudspeakers.Remove(uid);

        DoRemovalCheck(args.User, holder);
    }

    private void OnGetLoudspeakerHolder(Entity<LoudspeakerHolderComponent> ent, ref SpeechFontSizeOverrideEvent args)
    {
        foreach (var loudspeaker in ent.Comp.Loudspeakers)
        {
            var speechEv = new SpeechFontSizeOverrideEvent();
            RaiseLocalEvent(loudspeaker, ref speechEv);
            if (speechEv.IsActive)
            {
                args.IsActive = true;
                args.FontSize = speechEv.FontSize;
                args.AffectRadio = speechEv.AffectRadio;
                args.AffectChat = speechEv.AffectChat;
                args.SpeechSounds = speechEv.SpeechSounds;
                return;
            }
        }
    }

    private void OnGetLoudspeakerData(Entity<LoudspeakerComponent> ent, ref SpeechFontSizeOverrideEvent args)
    {
        args.IsActive = ent.Comp.IsActive;

        args.FontSize = ent.Comp.FontSize;
        args.AffectRadio = ent.Comp.AffectRadio;
        args.AffectChat = ent.Comp.AffectChat;
        args.SpeechSounds = ent.Comp.SpeechSounds;
    }

    private void OnGetSpeechSound(Entity<LoudspeakerHolderComponent> ent, ref GetSpeechSoundEvent args)
    {
        if (args.Handled)
            return;

        var ev = new SpeechFontSizeOverrideEvent();
        RaiseLocalEvent(ent, ref ev);

        if (ev.SpeechSounds is { })
        {
            args.SpeechSoundProtoId = ev.SpeechSounds;
            args.Handled = true;
        }
    }

    private void OnExamined(Entity<LoudspeakerComponent> ent, ref ExaminedEvent args)
    {
        var state = ent.Comp.IsActive ? "on" : "off";

        var message = ent.Comp.CanToggle
            ? Loc.GetString("loudspeaker-examine-toggleable", ("state", state))
            : Loc.GetString("loudspeaker-examine-generic");

        args.PushMarkup(message);
    }

    private void OnGetVerbs(Entity<LoudspeakerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract
            || !ent.Comp.CanToggle)
            return;

        var user = args.User;

        AlternativeVerb toggleLoudspeakerVerb = new()
        {
            Act = () =>
            {
                ToggleLoudspeakerEffect(user, ent);
                ent.Comp.IsActive = !ent.Comp.IsActive;
                Dirty(ent);
            },
            Text = Loc.GetString("loudspeaker-toggle"),
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Effects/text.rsi"), "exclamation"),
        };

        args.Verbs.Add(toggleLoudspeakerVerb);
    }

    #region Helper methods

    private void DoRemovalCheck(EntityUid equipee, LoudspeakerHolderComponent comp)
    {
        if (comp.Loudspeakers.Count == 0) // only remove when theres no loudspeakers
        {
            RemComp<LoudspeakerHolderComponent>(equipee);
            return;
        }
    }

    private void ToggleLoudspeakerEffect(EntityUid user, Entity<LoudspeakerComponent> loudspeaker)
    {
        var state = !loudspeaker.Comp.IsActive ? "on" : "off";

        _audio.PlayPredicted(loudspeaker.Comp.ToggleSound, user, user);
        _popup.PopupClient(Loc.GetString("loudspeaker-toggle-popup", ("state", state)), user, user);
    }

    #endregion
}
