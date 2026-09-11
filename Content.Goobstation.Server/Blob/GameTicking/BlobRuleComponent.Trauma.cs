// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Blob;
using Content.Server.GameTicking.Rules;
using Content.Shared.Mind;
using Robust.Shared.Audio;

namespace Content.Goobstation.Server.Blob.GameTicking;

public sealed partial class BlobRuleComponent
{
    [DataField]
    public SoundSpecifier? CriticalAudio = new SoundPathSpecifier("/Audio/_Trauma/StationEvents/assimilation.ogg");
}
