// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Common.Barks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpeechSynthesisComponent : Component
{
    [DataField("voice"), AutoNetworkedField]
    public ProtoId<BarkPrototype>? VoicePrototypeId;
}
