// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Speech;

[RegisterComponent]
[Access(typeof(SpeechRequiresEquipmentSystem))]
public sealed partial class SpeechRequiresEquipmentComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, EntityWhitelist> Equipment = [];

    [DataField]
    public LocId? FailMessage;
}
