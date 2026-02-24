// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Shared.Mind;

/// <summary>
/// Mind component that stores the last few chat messages someone sent.
/// </summary>
[RegisterComponent, Access(typeof(MindMessagesSystem))]
public sealed partial class MindMessagesComponent : Component
{
    /// <summary>
    /// Ring buffer of the last few messages.
    /// Array length is the max number of messages to keep.
    /// </summary>
    [DataField]
    public string[] Messages = [ string.Empty, string.Empty, string.Empty ];

    /// <summary>
    /// Ring buffer index, incremented and modulo'd every time a message is sent.
    /// </summary>
    [DataField]
    public int Index;
}
