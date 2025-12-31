// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Common.Audio;

/// <summary>
/// Component for sounds that will likely cause a copyright claim when streamed.
/// Automatically muted clientside if you have Streamer Mode enabled.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CopyrightedAudioComponent : Component;
