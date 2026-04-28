// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Shuttles.Events
{
    /// <summary>
    /// Sent when a network port button is pressed on the shuttle console.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ShuttlePortButtonPressedMessage : BoundUserInterfaceMessage
    {
        /// <summary>
        /// The source port identifier from the shuttle console.
        /// </summary>
        public string SourcePort { get; set; } = string.Empty;
    }
}
