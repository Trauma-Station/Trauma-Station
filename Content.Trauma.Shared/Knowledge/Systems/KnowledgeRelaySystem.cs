// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

/// <summary>
/// This class handles all the relay events
/// </summary>
public sealed partial class KnowledgeRelaySystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        // For knowledge specific events
        SubscribeLocalEvent<KnowledgeHolderComponent, GetAttackModifierEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetDefenseModifierEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetDamageModifierEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetStrengthFeatEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetAgilityFeatEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetDodgeSavingThrowEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetPhysicalSavingThrowEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetMentalSavingThrowEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetCarryLimitsEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetMoraleModifierEvent>(_knowledge.RelayEvent);

        // For body specific events
    }
}
