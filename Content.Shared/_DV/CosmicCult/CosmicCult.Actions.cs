using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._DV.CosmicCult;

[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicCultActionComponent : Component;

public sealed partial class EventCosmicSiphon : EntityTargetActionEvent;
public sealed partial class EventCosmicBlank : EntityTargetActionEvent;
public sealed partial class EventCosmicPlaceMonument : InstantActionEvent;
public sealed partial class EventCosmicReturn : InstantActionEvent;
public sealed partial class EventCosmicLapse : EntityTargetActionEvent;
public sealed partial class EventCosmicGlare : InstantActionEvent;
public sealed partial class EventCosmicIngress : EntityTargetActionEvent;
public sealed partial class EventCosmicImposition : InstantActionEvent;
public sealed partial class EventCosmicNova : WorldTargetActionEvent;
public sealed partial class EventCosmicFragmentation : EntityTargetActionEvent;
public sealed partial class EventCosmicShop : InstantActionEvent;
public sealed partial class EventCosmicConversion : EntityTargetActionEvent;
public sealed partial class EventCosmicDamageTransfer : EntityTargetActionEvent;
public sealed partial class EventCosmicTransmutation : InstantActionEvent;
public sealed partial class EventCosmicStride : InstantActionEvent;

// COLOSSUS ACTIONS
public sealed partial class EventCosmicColossusSunder : WorldTargetActionEvent;
public sealed partial class EventCosmicColossusIngress : EntityTargetActionEvent;
public sealed partial class EventCosmicColossusHibernate : InstantActionEvent;
public sealed partial class EventCosmicColossusEffigy : InstantActionEvent;
