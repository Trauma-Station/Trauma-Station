// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Enables and disables a mutation's radiation source when alive or dead.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RadiationMutationComponent : Component;
