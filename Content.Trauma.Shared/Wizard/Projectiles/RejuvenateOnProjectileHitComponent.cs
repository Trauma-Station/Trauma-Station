// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

<<<<<<<< HEAD:Content.Trauma.Shared/Wizard/Projectiles/RejuvenateOnProjectileHitComponent.cs
namespace Content.Trauma.Shared.Wizard.Projectiles;
========
namespace Content.Trauma.Shared.Heretic.Components;
>>>>>>>> upstream:Content.Trauma.Shared/Heretic/Components/EldritchInfluenceDrainerComponent.cs

[RegisterComponent, NetworkedComponent]
public sealed partial class RejuvenateOnProjectileHitComponent : Component
{
    [DataField]
    public EntityWhitelist UndeadList = new();

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool ReverseEffects;

    [DataField]
    public ProtoId<TagPrototype> SoulTappedTag = "SoulTapped";
}
