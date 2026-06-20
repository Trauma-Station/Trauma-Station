// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Body.Chips;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Installs an organ chip into the target mob.
/// </summary>
public sealed partial class InstallOrganChip : EntityEffectBase<InstallOrganChip>
{
    [DataField(required: true)]
    public EntProtoId<OrganChipComponent> Chip;
}

public sealed partial class InstallOrganChipEffectSystem : EntityEffectSystem<BodyComponent, InstallOrganChip>
{
    [Dependency] private OrganChipSystem _chip = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<InstallOrganChip> args)
    {
        _chip.InstallChip(ent, args.Effect.Chip);
    }
}
