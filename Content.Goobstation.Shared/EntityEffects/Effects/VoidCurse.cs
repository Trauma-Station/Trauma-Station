// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Heretic.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

public sealed partial class VoidCurse : EntityEffectBase<VoidCurse>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Inflicts void curse.";
}

public sealed class VoidCurseEffectSystem : EntityEffectSystem<TransformComponent, VoidCurse>
{
    [Dependency] private readonly SharedVoidCurseSystem _voidCurse = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<VoidCurse> args)
    {
        _voidCurse.DoCurse(ent);
    }
}
