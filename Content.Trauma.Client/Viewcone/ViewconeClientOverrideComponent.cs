// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Viewcone.Components;

namespace Content.Trauma.Client.Viewcone;

/// <summary>
/// Marks an entity which this client should always perceive, even if they have <see cref="ViewconeOccludableComponent"/>
/// </summary>
/// <remarks>
/// Used for dynamic situations where you should intuitively always show the occludable, like if you're pulling it.
/// </remarks>
[RegisterComponent]
public sealed partial class ViewconeClientOverrideComponent : Component;
