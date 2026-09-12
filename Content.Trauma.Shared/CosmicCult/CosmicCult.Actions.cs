// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.CosmicCult;

[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicCultActionComponent : Component;

public sealed partial class CosmicSiphonEvent : EntityTargetActionEvent;
public sealed partial class CosmicBlankEvent : EntityTargetActionEvent;
public sealed partial class CosmicPlaceMonumentEvent : InstantActionEvent;
public sealed partial class CosmicReturnEvent : InstantActionEvent;
public sealed partial class CosmicLapseEvent : EntityTargetActionEvent;
public sealed partial class CosmicGlareEvent : InstantActionEvent;
public sealed partial class CosmicIngressEvent : EntityTargetActionEvent;
public sealed partial class CosmicImpositionEvent : InstantActionEvent;
public sealed partial class CosmicNovaEvent : WorldTargetActionEvent;
public sealed partial class CosmicFragmentationEvent : EntityTargetActionEvent;
public sealed partial class CosmicShopEvent : InstantActionEvent;
public sealed partial class CosmicConversionEvent : EntityTargetActionEvent;
public sealed partial class CosmicDamageTransferEvent : EntityTargetActionEvent;
public sealed partial class CosmicTransmutationEvent : InstantActionEvent;
public sealed partial class CosmicStrideEvent : InstantActionEvent;

// COLOSSUS ACTIONS
public sealed partial class CosmicColossusSunderEvent : WorldTargetActionEvent;
public sealed partial class CosmicColossusIngressEvent : EntityTargetActionEvent;
public sealed partial class CosmicColossusHibernateEvent : InstantActionEvent;
public sealed partial class CosmicColossusEffigyEvent : InstantActionEvent;
