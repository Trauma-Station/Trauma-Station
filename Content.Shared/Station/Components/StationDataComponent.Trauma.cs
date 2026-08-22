namespace Content.Shared.Station.Components;

public sealed partial class StationDataComponent
{
    /// <summary>
    /// List of all grids for this station that have <c>BecomesStationComponent</c>.
    /// Unlike <see cref="Grids"/> it does not include members like ATS, cargo shuttle or split grids (engi shittles usually)
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> OwnedGrids = new();
}
