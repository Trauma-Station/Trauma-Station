// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Robust.Shared.Audio;

namespace Content.Medical.Shared.Tourniquet;

/// <summary>
/// This is used for tourniquet. Yes
/// </summary>
[RegisterComponent]
public sealed partial class TourniquetComponent : Component
{
    [DataField]
    public EntityUid? BodyPartTorniqueted;

    /// <summary>
    /// How long it takes to put the tourniquet on.
    /// </summary>
    [DataField]
    public float Delay = 5f;

    /// <summary>
    /// How long it takes to take the tourniquet off.
    /// </summary>
    [DataField]
    public float RemoveDelay = 7f;

    [DataField]
    public List<BodyPartType> BlockedBodyParts = new();

    /// <summary>
    ///     Sound played on healing begin
    /// </summary>
    [DataField]
    public SoundSpecifier? PutOnSound = null;

    /// <summary>
    ///     Sound played on healing end
    /// </summary>
    [DataField]
    public SoundSpecifier? PutOffSound = null;
}
