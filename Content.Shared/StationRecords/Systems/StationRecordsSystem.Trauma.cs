using System.Text;

namespace Content.Shared.StationRecords.Systems;

public sealed partial class StationRecordsSystem
{
    private bool IsFilterWithSomeCodeValue(string value, string filter)
    {
        var filterletList = ApplyWildcard(filter);

        //NOTE TO SELF: IF TRUE, FILTER THIS ENTRY
        //SECOND NOTE TO SELF: ALL FILTERS NEED TO RETURN TRUE, THEN FINALLY RETURN FALSE
        bool allFiltersPassed = true;
        foreach (var (filterlet, cutoff) in filterletList)
        {
            allFiltersPassed &= value.Substring(cutoff).ToLower().StartsWith(filterlet);
        }

        return !allFiltersPassed;

        //OG Logic
        //return !value.ToLower().StartsWith(filter);
    }

    /// <summary>
    /// This helper method chops a filter into a list of filterlets and indexes.
    /// Indexes must be provided because we can only match the start of a string
    /// </summary>
    /// <param name="filter"> The thing to be slam-chopped </param>
    /// <returns>A list of filterlets and the index they come from</returns>
    private List<(string, int)> ApplyWildcard(string filter)
    {
        var filterList = new List<(string, int)>();
        var filterlet = new StringBuilder();
        int segmentStart = 0;
        int index = 0;

        foreach (var c in filter)
        {
            if (c == '#')
            {
                if (filterlet.Length > 0) // The current filterlet string is finished, so-
                {
                    filterList.Add((filterlet.ToString(), segmentStart)); // -save the filterlet-
                    filterlet.Clear(); // -and start search for a new one
                }
            }
            else
            {
                if (filterlet.Length == 0)
                {
                    // This is the start of a new segment
                    segmentStart = index;
                }

                filterlet.Append(c); // ###F##D8
            }

            index++;
        }

        // Don't forget the last segment
        if (filterlet.Length > 0)
        {
            filterList.Add((filterlet.ToString(), segmentStart));
        }

        return filterList;
    }
}
