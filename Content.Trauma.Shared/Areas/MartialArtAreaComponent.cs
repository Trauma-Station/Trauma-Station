// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Common.MartialArts;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// For <c>Blocked</c> martial arts, allows it to be used in this area.
/// Aka chefs can use CQC in the kitchen.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MartialArtAreaSystem))]
public sealed partial class MartialArtAreaComponent : Component
{
    /// <summary>
    /// The martial art form allowed by this area.
    /// </summary>
    [DataField(required: true)]
    public MartialArtsForms Form = default!;
}
