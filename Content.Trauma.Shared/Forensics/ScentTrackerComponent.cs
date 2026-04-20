// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Forensics
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ScentTrackerComponent : Component
    {
        /// <summary>
        ///     The currently tracked scent.
        /// </summary>
        [DataField, AutoNetworkedField]
        public string Scent = string.Empty;

        /// <summary>
        ///     The time (in seconds) that it takes to sniff an entity.
        /// </summary>
        [DataField]
        public float SniffDelay = 5.0f;
    }
}
