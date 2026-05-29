// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.TapeRecorder;

/// <summary>
/// Removed from the cassette when damaged to prevent it being played until repaired
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FitsInTapeRecorderComponent : Component;
