// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Lavaland.Shared.Audio;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class BossMusicComponent : Component
{
    [AutoNetworkedField]
    [DataField] public ProtoId<BossMusicPrototype> SoundId;
}
