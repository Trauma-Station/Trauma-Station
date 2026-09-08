// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a temporary combat modifier to the target's active martial art.
/// </summary>
public sealed partial class ApplyMartialArtModifier : EntityEffectBase<ApplyMartialArtModifier>
{
    [DataField]
    public MartialArtModifierType Type = MartialArtModifierType.AttackRate;

    [DataField]
    public float Multiplier = 1f;

    [DataField]
    public float Modifier;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3);
}

public sealed partial class ApplyMartialArtModifierSystem : EntityEffectSystem<MetaDataComponent, ApplyMartialArtModifier>
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private MartialArtsSystem _martialArts = default!;
    [Dependency] private EntityQuery<MartialArtModifiersComponent> _modifiersQuery = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<ApplyMartialArtModifier> args)
    {
        if (_knowledge.GetActiveMartialArt(ent.Owner) is not { } art
            || !_modifiersQuery.TryComp(art, out var modifiers))
            return;

        var effect = args.Effect;
        _martialArts.ApplyModifier((art, modifiers),
            effect.Type,
            effect.Multiplier,
            effect.Modifier,
            effect.Duration,
            ent.Owner);
    }
}
