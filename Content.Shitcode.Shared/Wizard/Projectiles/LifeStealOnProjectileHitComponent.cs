<<<<<<<< HEAD:Content.Shitcode.Shared/Wizard/Projectiles/LifeStealOnProjectileHitComponent.cs
========
>>>>>>>> upstream:Content.Trauma.Shared/Heretic/Components/PathSpecific/Rust/EntropicPlumeComponent.cs
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

<<<<<<<< HEAD:Content.Shitcode.Shared/Wizard/Projectiles/LifeStealOnProjectileHitComponent.cs
namespace Content.Shitcode.Shared.Wizard.Projectiles;
========
namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;
>>>>>>>> upstream:Content.Trauma.Shared/Heretic/Components/PathSpecific/Rust/EntropicPlumeComponent.cs

[RegisterComponent, NetworkedComponent]
public sealed partial class LifeStealOnProjectileHitComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist;

    [DataField]
    public FixedPoint2 LifeStealAmount = 20;

    [DataField]
    public FixedPoint2 BloodStealAmount = 25;

    [DataField]
    public EntProtoId Effect = "SanguineBloodEffect";
}
