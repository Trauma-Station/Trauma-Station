// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent]
public sealed partial class StarTouchComponent : Component
{
    [DataField]
    public TimeSpan DrowsinessTime = TimeSpan.FromSeconds(8);

    [DataField]
    public SpriteSpecifier BeamSprite = new SpriteSpecifier.Rsi(new("/Textures/_Goobstation/Heretic/Effects/effects.rsi"), "cosmic_beam");

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(8);

    [DataField]
    public float CosmicFieldLifetime = 30f;
}
