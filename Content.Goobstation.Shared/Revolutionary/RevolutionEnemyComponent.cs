// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;
using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.Revolutionary;

[RegisterComponent, NetworkedComponent]
public sealed partial class RevolutionEnemyComponent : Component
{
    /// <summary>
    /// The status icon prototype displayed for revolutionaries
    /// </summary>
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "RevolutionEnemy";

    /// <summary>
    /// Sound that plays when you are chosen as Rev. (Placeholder until I find something cool I guess)
    /// </summary>
    [DataField]
    public SoundSpecifier RevStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/headrev_start.ogg");

    public override bool SessionSpecific => true;
}
