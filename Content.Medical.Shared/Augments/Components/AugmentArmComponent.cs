// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Medical.Shared.Augments;

/// <summary>
/// Marker component to indicate that an entity serves as an AugmentArm organ
/// <summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AugmentArmComponent : Component;
