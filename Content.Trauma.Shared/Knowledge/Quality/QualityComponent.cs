// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Quality;

namespace Content.Trauma.Shared.Knowledge.Quality;

/// <summary>
/// Stores the quality info of an object, added when a player crafts/constructs it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class QualityComponent : Component
{
    /// <summary>
    /// Stores the level mastery of the item that will satisfactorily modify it.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<EntProtoId, int> LevelDeltas = new();

    /// <summary>
    /// Stores the quality of the item, which changes some functionality when used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Quality = 0;

    /// <summary>
    /// Stores any particular modifiers used for the next roll.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int QualityModifiers = 0;

    /// <summary>
    /// Stores the ID of item coefficients.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<QualityPrototype> QualityFactors = "BaseQuality";
}
