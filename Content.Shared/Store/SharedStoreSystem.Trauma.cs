using Content.Trauma.Common.JobListings;

namespace Content.Shared.Store;

public abstract partial class SharedStoreSystem
{
    [Dependency] private SharedJobListingsSystem _jobListings = default!;
}
