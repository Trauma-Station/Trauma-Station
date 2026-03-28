<<<<<<<< HEAD:Content.Shitcode.Shared/Wizard/Projectiles/SwapOnProjectileHitComponent.cs
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
========
>>>>>>>> upstream:Content.Trauma.Shared/Heretic/Components/Side/Carvings/AlertCarvingComponent.cs
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

<<<<<<<< HEAD:Content.Shitcode.Shared/Wizard/Projectiles/SwapOnProjectileHitComponent.cs
namespace Content.Shitcode.Shared.Wizard.Projectiles;
========
namespace Content.Trauma.Shared.Heretic.Components.Side.Carvings;
>>>>>>>> upstream:Content.Trauma.Shared/Heretic/Components/Side/Carvings/AlertCarvingComponent.cs

[RegisterComponent, NetworkedComponent]
public sealed partial class SwapOnProjectileHitComponent : Component
{
    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public EntProtoId Effect = "SwapSpellEffect";

    [DataField]
    public EntityWhitelist Whitelist;

    [DataField]
    public bool DeleteProjectileOnSwap;
}
