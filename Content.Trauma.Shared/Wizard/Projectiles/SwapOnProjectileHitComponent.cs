// SPDX-License-Identifier: AGPL-3.0-or-later

<<<<<<<< HEAD:Content.Trauma.Shared/Wizard/Projectiles/SwapOnProjectileHitComponent.cs
========
>>>>>>>> upstream:Content.Trauma.Shared/Heretic/Components/Side/Carvings/AlertCarvingComponent.cs

using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

<<<<<<<< HEAD:Content.Trauma.Shared/Wizard/Projectiles/SwapOnProjectileHitComponent.cs
namespace Content.Trauma.Shared.Wizard.Projectiles;
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
