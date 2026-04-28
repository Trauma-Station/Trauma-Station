// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.CosmicCult.Components;

/// <summary>
/// Component for displaying Vacuous Imposition's visuals on a player.
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicImposingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Expiry;

    [DataField]
    public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(new("/Textures/_DV/CosmicCult/Effects/ability_imposition_overlay.rsi"), "vfx");
}

[Serializable, NetSerializable]
public enum CosmicImposingKey
{
    Key
}
