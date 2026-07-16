// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Glue;

/// <summary>
/// Component for mobs or gloves that makes them unaffected by glued items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GlueImmuneComponent : Component;
