// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Speech;

/// <summary>
/// Accent that makes you speak in pig latin.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PigLatinAccentComponent : Component;
