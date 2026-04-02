using System;
using System.Collections.Generic;
using System.Text;
using Content.Trauma.Common.CartridgeLoader.Cartridges;

namespace Content.Shared.CartridgeLoader.Cartridges;

public sealed partial class LogProbeUiState
{
    /// <summary>
    /// DeltaV: The NanoChat data if a card was scanned, null otherwise
    /// </summary>
    public NanoChatData? NanoChatData { get; }
}
