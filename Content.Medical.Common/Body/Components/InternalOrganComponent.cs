// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.FixedPoint;
using Content.Medical.Common.Surgery.Tools;
using Content.Medical.Common.Traumas;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Medical.Common.Body;

/// <summary>
/// Organ component specifically for internal organs, e.g. lungs, heart.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class InternalOrganComponent : Component, ISurgeryToolComponent
{
    #region ISurgeryToolComponent

    [DataField]
    public string ToolName { get; set; } = "An organ";

    [DataField]
    public float Speed { get; set; } = 1f;

    [DataField]
    public bool? Used { get; set; }

    #endregion

    /// <summary>
    ///     Maximum organ integrity, do keep in mind that Organs are supposed to be VERY and VERY damage sensitive
    /// </summary>
    [DataField("intCap"), AutoNetworkedField]
    public FixedPoint2 IntegrityCap = 15;

    /// <summary>
    ///     Current organ HP, or integrity, whatever you prefer to say
    /// </summary>
    [DataField("integrity"), AutoNetworkedField]
    public FixedPoint2 OrganIntegrity = 15;

    /// <summary>
    ///     Current Organ severity, dynamically updated based on organ integrity
    /// </summary>
    [DataField, AutoNetworkedField]
    public OrganSeverity OrganSeverity = OrganSeverity.Normal;

    /// <summary>
    ///     Sound played when this organ gets turned into a blood mush.
    /// </summary>
    [DataField]
    public SoundSpecifier OrganDestroyedSound = new SoundCollectionSpecifier("OrganDestroyed");

    /// <summary>
    ///     All the modifiers that are currently modifying the OrganIntegrity
    /// </summary>
    public Dictionary<(string, EntityUid), FixedPoint2> IntegrityModifiers = new();

    /// <summary>
    ///     The name's self-explanatory, thresholds. for states. of integrity. of this god fucking damn organ.
    /// </summary>
    [DataField] // TODO SHITMED: MAKE REQUIRED WHEN EVERY YML HAS THESE.
    public Dictionary<OrganSeverity, FixedPoint2> IntegrityThresholds = new()
    {
        { OrganSeverity.Normal, 15 },
        { OrganSeverity.Damaged, 10 },
        { OrganSeverity.Destroyed, 0 },
    };
}
