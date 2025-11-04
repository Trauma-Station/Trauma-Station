// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Removes access provided by the target entity ID card.
/// If the target entity is a mob or PDA it will look for a PDA or ID in its hands or ID slot instead.
/// </summary>
public sealed partial class RemoveAccess : EntityEffectBase<RemoveAccess>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Removes all target access.";
}

public sealed class RemoveAccessEffectSystem : EntityEffectSystem<TransformComponent, RemoveAccess>
{
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<RemoveAccess> args)
    {
        if (!_idCard.TryFindIdCard(ent, out var id))
            return;

        _access.TrySetTags(id, new List<ProtoId<AccessLevelPrototype>>());
    }
}
