using Content.Shared.Strip;

namespace Content.Shared.Storage.EntitySystems;

public abstract partial class SharedStorageSystem
{
    [Dependency] private ThievingSystem _thieving = default!;
}
