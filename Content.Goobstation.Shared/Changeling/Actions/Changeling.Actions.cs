// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Changeling.Actions;

#region Events - Basic

public sealed partial class OpenEvolutionMenuEvent : InstantActionEvent;
public sealed partial class AbsorbDNAEvent : EntityTargetActionEvent;
public sealed partial class AbsorbBiomatterEvent : EntityTargetActionEvent;
public sealed partial class StingExtractDNAEvent : EntityTargetActionEvent;
public sealed partial class ChangelingTransformCycleEvent : InstantActionEvent;
public sealed partial class ChangelingTransformEvent : InstantActionEvent;
public sealed partial class EnterStasisEvent : InstantActionEvent;
public sealed partial class ExitStasisEvent : InstantActionEvent;

#endregion

#region Events - Combat

public sealed partial class LingToggleItemEvent : InstantActionEvent
{
    [DataField(required: true)]
    public EntProtoId Item;
}
public sealed partial class ToggleDartGunEvent : InstantActionEvent;
public sealed partial class CreateBoneShardEvent : InstantActionEvent;
public sealed partial class ToggleChitinousArmorEvent : InstantActionEvent;
public sealed partial class ShriekDissonantEvent : InstantActionEvent;
public sealed partial class ShriekResonantEvent : InstantActionEvent;
public sealed partial class ToggleStrainedMusclesEvent : InstantActionEvent;

#endregion

#region Events - Sting

public sealed partial class StingReagentEvent : EntityTargetActionEvent;
public sealed partial class StingFakeArmbladeEvent : EntityTargetActionEvent;
public sealed partial class StingTransformEvent : EntityTargetActionEvent;
public sealed partial class StingLayEggsEvent : EntityTargetActionEvent;

#endregion

#region Events - Utility

public sealed partial class ActionAnatomicPanaceaEvent : InstantActionEvent;
public sealed partial class ActionBiodegradeEvent : InstantActionEvent;
public sealed partial class ActionChameleonSkinEvent : InstantActionEvent;
public sealed partial class ActionAdrenalineReservesEvent : InstantActionEvent;
public sealed partial class ActionFleshmendEvent : InstantActionEvent
{
    [DataField]
    public ProtoId<StatusEffectPrototype> StatusID = "Fleshmend";

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);
}
public sealed partial class ActionLastResortEvent : InstantActionEvent;
public sealed partial class ActionLesserFormEvent : InstantActionEvent;
public sealed partial class ActionVoidAdaptEvent : InstantActionEvent;
public sealed partial class ActionHivemindAccessEvent : InstantActionEvent;
public sealed partial class ActionContortBodyEvent : InstantActionEvent;

#endregion

#region Events - Misc

[DataDefinition]
public sealed partial class AugmentedEyesightPurchasedEvent : EntityEventArgs;

[DataDefinition]
public sealed partial class AwakenedInstinctPurchasedEvent : EntityEventArgs;

#endregion
