using Content.Shared.Bed.Sleep;

namespace Content.Shared.Medical.Cryogenics;

public abstract partial class SharedCryoPodSystem
{
    [Dependency] private readonly SleepingSystem _sleeping = default!;
}
