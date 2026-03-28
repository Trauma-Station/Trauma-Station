// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Heretic.Components.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class CarvingAlertedStatusEffectComponent : Component
{
    [DataField]
    public Dictionary<NetCoordinates, EntityUid> Locations = new();

    public const string Id = "alertcarving";

    [DataField]
    public SoundSpecifier? TeleportSound = new SoundCollectionSpecifier("Curse");
}
