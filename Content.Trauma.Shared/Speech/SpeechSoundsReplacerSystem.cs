// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory.Events;
using Content.Shared.Speech;

namespace Content.Trauma.Shared.Speech;

/// <summary>
/// System that replace your speech sound when you wearing specific clothing
/// </summary>
public sealed class SpeechSoundsReplacerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechSoundsReplacerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<SpeechSoundsReplacerComponent, GotUnequippedEvent>(OnUnequip);
    }

    private void OnEquip(Entity<SpeechSoundsReplacerComponent> replacer, ref GotEquippedEvent args)
    {
        if (!TryComp<SpeechComponent>(args.Equipee, out var speech))
            return;

        replacer.Comp.PreviousSound = speech.SpeechSounds;
        speech.SpeechSounds = replacer.Comp.SpeechSounds;
        Dirty(replacer);
        Dirty(args.Equipee, speech);
    }

    private void OnUnequip(Entity<SpeechSoundsReplacerComponent> replacer, ref GotUnequippedEvent args)
    {
        if (!TryComp<SpeechComponent>(args.Equipee, out var speech))
            return;

        speech.SpeechSounds = replacer.Comp.PreviousSound;
        replacer.Comp.PreviousSound = null;
        Dirty(replacer);
        Dirty(args.Equipee, speech);
    }
}
