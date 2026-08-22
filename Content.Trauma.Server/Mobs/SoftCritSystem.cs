// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio;
using Content.Trauma.Shared.Mobs;

namespace Content.Trauma.Server.Mobs;

/// <summary>
/// Prevents screaming while in softcrit, you can only whisper chud.
/// </summary>
public sealed partial class SoftCritSystem : SharedSoftCritSystem
{
    // event in server for no reason award
    [SubscribeLocalEvent]
    private void OnRadioSendAttempt(Entity<SoftCritMobComponent> ent, ref RadioSendAttemptEvent args)
    {
        args.Cancelled = true; // no yapping on radio chuddy
    }
}
