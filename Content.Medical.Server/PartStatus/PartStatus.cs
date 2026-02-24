// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Medical.Common.Wounds;

namespace Content.Medical.Server.PartStatus;

// collecting a body parts information together
// ik its another bs level of abstraction but i think it helps for now..
public sealed class PartStatus(
    BodyPartType partType,
    BodyPartSymmetry partSymmetry,
    string partName,
    WoundableSeverity partSeverity,
    Dictionary<string, WoundSeverity> damageSeverities,
    BoneSeverity boneSeverity,
    bool bleeding)
{
    public BodyPartType PartType = partType;

    public BodyPartSymmetry PartSymmetry = partSymmetry;

    public string PartName = partName;

    public WoundableSeverity PartSeverity = partSeverity;

    public Dictionary<string, WoundSeverity> DamageSeverities = damageSeverities;

    public BoneSeverity BoneSeverity = boneSeverity;

    public bool Bleeding = bleeding;
}
