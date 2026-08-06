// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Guardian.Components;

/// <summary>
/// Configures the Lightning holoparasite variant. While manifested it behaves like a ball of
/// lightning: passive bolts periodically arc to nearby enemies without stunning, the active
/// action fires a targeted bolt that stuns and then chains to other enemies, and while the
/// guardian exists its host is granted electricity resistance (a gene and a Shock modifier set).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GuardianLightningComponent : Component
{
    // Passive arcs
    /// <summary>
    /// Beam prototype used for the passive arcs around the guardian.
    /// </summary>
    [DataField]
    public EntProtoId PassiveProto = "GuardianLightningPassive";

    /// <summary>
    /// How often a passive arc fires while the guardian is manifested.
    /// </summary>
    [DataField]
    public TimeSpan PassiveTick = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Selection radius for passive arc targets around the guardian.
    /// </summary>
    [DataField]
    public float PassiveRange = 4f;

    /// <summary>
    /// How many passive bolts fire per tick.
    /// </summary>
    [DataField]
    public int PassiveBoltCount = 1;

    /// <summary>
    /// How many times a passive bolt bounces from target to target.
    /// </summary>
    [DataField]
    public int PassiveArcDepth = 3;

    /// <summary>
    /// Shock damage dealt by each passive bolt. Passive bolts never stun.
    /// </summary>
    [DataField]
    public float PassiveDamage = 10f;

    // Active bolt
    /// <summary>
    /// Beam prototype used for the targeted bolt action.
    /// </summary>
    [DataField]
    public EntProtoId BoltProto = "GuardianLightning";

    /// <summary>
    /// Shock damage dealt by the active bolt.
    /// </summary>
    [DataField]
    public float BoltDamage = 25f;

    /// <summary>
    /// Paralyze time, in seconds, applied by the active bolt.
    /// </summary>
    [DataField]
    public float BoltStunTime = 5f;

    [DataField]
    public float BoltRange = 7f;

    /// <summary>
    /// How far the active bolt chains from its primary target to nearby enemies.
    /// </summary>
    [DataField]
    public float BoltChainRange = 4f;

    /// <summary>
    /// How many times the active bolt bounces from enemy to enemy after the primary target.
    /// </summary>
    [DataField]
    public int BoltChainDepth = 2;

    // Host protection
    /// <summary>
    /// Mutation granted to the host while the guardian exists, providing electricity resistance.
    /// </summary>
    [DataField]
    public EntProtoId<MutationComponent> GeneId = "MutationInsulated";

    /// <summary>
    /// Damage modifier set applied to the host while the guardian exists. Use a
    /// DamageModifierSetPrototype with a Shock coefficient of 0 for full insulation.
    /// </summary>
    [DataField]
    public ProtoId<DamageModifierSetPrototype>? HostDamageModifierSet = new("GuardianLightningHost");

    // Runtime state (server only)
    /// <summary>
    /// Next time a passive arc fires.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextPassive;

    /// <summary>
    /// Host the protection was last applied to, so protection can be moved when the host
    /// changes (e.g. the host is polymorphed) and cleaned up on shutdown.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ProtectedHost;
}
