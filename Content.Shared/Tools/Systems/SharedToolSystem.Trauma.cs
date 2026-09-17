using Content.Shared.Timing.Systems;

namespace Content.Shared.Tools.Systems;

public abstract partial class SharedToolSystem
{
    [Dependency] private UseDelaySystem _delay = default!;
}
