// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Server.Radio;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;
using Content.Trauma.Shared.Mobs;

namespace Content.Trauma.Server.Mobs;

/// <summary>
/// Prevents screaming while in softcrit, you can only whisper chud.
/// </summary>
public sealed class SoftCritSystem : SharedSoftCritSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoftCritMobComponent, ScreamActionEvent>(OnScreamAction, before: new[] { typeof(VocalSystem) });
        SubscribeLocalEvent<SoftCritMobComponent, RadioSendAttemptEvent>(OnRadioSendAttempt); // event in server for no reason award
    }

    private void OnScreamAction(Entity<SoftCritMobComponent> ent, ref ScreamActionEvent args)
    {
        args.Handled = true; // shush
    }

    private void OnRadioSendAttempt(Entity<SoftCritMobComponent> ent, ref RadioSendAttemptEvent args)
    {
        args.Cancelled = true; // no yapping on radio chuddy
    }
}
