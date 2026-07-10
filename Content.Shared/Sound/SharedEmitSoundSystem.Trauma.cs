using Content.Shared.Strip;

namespace Content.Shared.Sound;

public abstract partial class SharedEmitSoundSystem
{
    [Dependency] private ThievingSystem _thieving = default!;
}
