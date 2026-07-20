// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Heretic.Components.Ghoul;

[RegisterComponent, NetworkedComponent]
public sealed partial class LordOfTheNightComponent : Component
{

    /// <summary>
    /// Set to true when flesh heretic mind gets added.
    /// Used to apply FleshHereticMindComponent SustainedDamage to the worm
    /// </summary>
    [DataField]
    public bool HereticInitialized;

    [DataField]
    public EntityWhitelist ArmWhitelist = new()
    {
        Components = new[] { "Arm" },
    };

    [DataField]
    public EntityWhitelist UnanchorWhitelist = new()
    {
        Components = new[] { "Door", "Destructible" },
        Tags = new() { "Wall", "Window", "Structure" },
    };

    [DataField]
    public EntityWhitelist PushBlacklist = new()
    {
        Tags = new() { "FleshWormSegment" },
    };

    [DataField]
    public FixedPoint2 HealPerArm = 80;

    [DataField]
    public FixedPoint2 HealthPerSegment = 250;

    [DataField]
    public float ArmDelimbChance = 0.25f;

    [DataField]
    public ProtoId<OrganCategoryPrototype> ArmLeft = "ArmLeft";

    [DataField]
    public ProtoId<OrganCategoryPrototype> ArmRight = "ArmRight";

    [DataField(required: true)]
    public EntityEffect[] MadnessEffects;

    [DataField]
    public float MadnessRange = 8f;

    [DataField]
    public EntProtoId TransformAction = "ActionHereticFleshTransform";

    [DataField]
    public float ForceMultiplier = 0.1f;
}
