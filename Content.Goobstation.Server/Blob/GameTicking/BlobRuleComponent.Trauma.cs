// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Goobstation.Server.Blob.GameTicking;

public sealed partial class BlobRuleComponent
{
    [DataField]
    public SoundSpecifier? CriticalAudio = new SoundPathSpecifier("/Audio/_Trauma/StationEvents/assimilation.ogg");
}
