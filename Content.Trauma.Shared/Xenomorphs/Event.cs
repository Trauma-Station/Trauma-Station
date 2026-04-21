// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Trauma.Shared.Xenomorphs.Caste;
using Content.Shared.Actions;
using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Xenomorphs;

[Serializable, NetSerializable]
public sealed partial class XenomorphEvolutionDoAfterEvent : DoAfterEvent
{
    [DataField]
    public EntProtoId Choice;

    [DataField]
    public ProtoId<XenomorphCastePrototype> Caste;

    [DataField]
    public bool CheckNeedCasteDeath;

    public XenomorphEvolutionDoAfterEvent(EntProtoId choice, ProtoId<XenomorphCastePrototype> caste, bool checkNeedCasteDeath = true)
    {
        Choice = choice;
        Caste = caste;
        CheckNeedCasteDeath = checkNeedCasteDeath;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class LarvaBurstDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class QueenRoarDoAfterEvent : SimpleDoAfterEvent;

public sealed partial class ToggleAcidSpitEvent : InstantActionEvent;

public sealed partial class FaceHuggerLeapActionEvent : WorldTargetActionEvent;

public sealed partial class QueenRoarActionEvent : InstantActionEvent;


public sealed partial class TransferPlasmaActionEvent : EntityTargetActionEvent
{
    [DataField]
    public FixedPoint2 Amount = 50;
}

public sealed partial class EvolutionsActionEvent : InstantActionEvent;

public sealed partial class PromotionActionEvent : EntityTargetActionEvent
{
    // Target is already provided by EntityTargetActionEvent
}

public sealed partial class TailLashActionEvent : WorldTargetActionEvent;

public sealed partial class AcidActionEvent : EntityTargetActionEvent;

[ByRefEvent]
public record struct AfterXenomorphEvolutionEvent(EntityUid EvolvedInto, EntityUid MindUid, ProtoId<XenomorphCastePrototype> Caste);

[ByRefEvent]
public record struct BeforeXenomorphEvolutionEvent(ProtoId<XenomorphCastePrototype> Caste, bool CheckNeedCasteDeath = true, bool Cancelled = false);

[ByRefEvent]
public record struct PlasmaAmountChangeEvent(FixedPoint2 Amount);
