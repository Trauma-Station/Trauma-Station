// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Abilities.Mime;
using Content.Trauma.Common.Abilities.Mime;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Abilities.Mime;

/// <summary>
/// Runs a YML-defined entity effect on mimes when they break vows.
/// </summary>
public sealed class MimePunishmentSystem : EntitySystem
{
    [Dependency] private readonly NestedEffectSystem _nestedEffect = default!;

    public static readonly ProtoId<EntityEffectPrototype> Punishments = "MimePunishments";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, MimeBrokeVowEvent>(OnBrokeVow);
    }

    private void OnBrokeVow(Entity<MimePowersComponent> ent, ref MimeBrokeVowEvent args)
    {
        _nestedEffect.ApplyNestedEffect(ent, Punishments, user: ent.Owner);
    }
}
