// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;
using Content.Trauma.Shared.Mobs;

namespace Content.Trauma.Server.Mobs;

/// <summary>
/// Prevents screaming while in softcrit, you can only whisper chud.
/// </summary>
public sealed partial class SoftCritSystem : SharedSoftCritSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoftCritMobComponent, EmoteActionEvent>(OnEmoteAction, before: new[] { typeof(VocalSystem) });
    }

    private void OnEmoteAction(Entity<SoftCritMobComponent> ent, ref EmoteActionEvent args)
    {
        args.Handled = true; // shush
    }

    // event in server for no reason award
    [SubscribeLocalEvent]
    private void OnRadioSendAttempt(Entity<SoftCritMobComponent> ent, ref RadioSendAttemptEvent args)
    {
        args.Cancelled = true; // no yapping on radio chuddy
    }
}
