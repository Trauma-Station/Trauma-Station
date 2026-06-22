// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Lavaland.Shared.Megafauna.Components;

/// <summary>
/// Makes megafauna immune to damage without an origin and any damage while AI is inactive.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MegafaunaGodmodeComponent : Component;
