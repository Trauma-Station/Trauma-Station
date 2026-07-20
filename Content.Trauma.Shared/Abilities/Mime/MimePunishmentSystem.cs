// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Abilities.Mime;
using Content.Shared.EntityEffects;
using Content.Trauma.Common.Abilities.Mime;

namespace Content.Trauma.Shared.Abilities.Mime;

/// <summary>
/// Runs a YML-defined entity effect on mimes when they break vows.
/// </summary>
public sealed partial class MimePunishmentSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    public static readonly ProtoId<EntityEffectPrototype> Punishments = "MimePunishments";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, MimeBrokeVowEvent>(OnBrokeVow);
    }

    private void OnBrokeVow(Entity<MimePowersComponent> ent, ref MimeBrokeVowEvent args)
    {
        _effects.TryApplyEffect(ent, Punishments, user: ent.Owner);
    }
}
