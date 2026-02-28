// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Common.MartialArts;

[RegisterComponent]
public sealed partial class MartialArtBlockedComponent : Component
{
    [DataField]
    public MartialArtsForms Form;
}
public abstract partial class GrabStagesOverrideComponent : Component
{
    public GrabStage StartingStage = GrabStage.Soft;
}

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MartialArtsKnowledgeComponent : GrabStagesOverrideComponent
{
    [DataField]
    [AutoNetworkedField]
    public MartialArtsForms MartialArtsForm = MartialArtsForms.CloseQuartersCombat;

    [DataField]
    [AutoNetworkedField]
    public bool Blocked;

    [DataField]
    [AutoNetworkedField]
    public float OriginalFistDamage;

    [DataField]
    [AutoNetworkedField]
    public string OriginalFistDamageType;

}

[Serializable, NetSerializable]
public enum MartialArtsForms
{
    CorporateJudo,
    CloseQuartersCombat,
    SleepingCarp,
    Capoeira,
    KungFuDragon,
    Ninjutsu,
    HellRip,
}
