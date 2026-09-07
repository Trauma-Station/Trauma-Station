// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Salvage;

/// <summary>
/// Component that fultons hit entities with the gun's <c>FultonComponent</c> data.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FultonProjectileComponent : Component
{
    /// <summary>
    /// Sound played if the hit entity can't be fultoned or the gun has no beacon.
    /// </summary>
    [DataField]
    public SoundSpecifier? PopSound = new SoundPathSpecifier("/Audio/Effects/balloon-pop.ogg");
}
