using Content.Server.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Presets
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    [Prototype]
    public sealed partial class GamePresetPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField]
        public string[] Alias = Array.Empty<string>();

        [DataField("name", required: true)] // Trauma - required
        public LocId ModeTitle; // Trauma - change to LocId

        [DataField(required: true)] // Trauma - required
        public LocId Description; // Trauma - change to LocId

        [DataField]
        public bool ShowInVote;

        [DataField]
        public int? MinPlayers;

        [DataField]
        public int? MaxPlayers;

        [DataField]
        public IReadOnlyList<EntProtoId> Rules { get; private set; } = Array.Empty<EntProtoId>();

        /// <summary>
        /// If specified, the gamemode will only be run with these maps.
        /// If none are elligible, the global fallback will be used.
        /// </summary>
        [DataField("supportedMaps")]
        public ProtoId<GameMapPoolPrototype>? MapPool;
    }
}
