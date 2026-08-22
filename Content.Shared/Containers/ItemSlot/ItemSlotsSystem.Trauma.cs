using Content.Shared.Strip;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    [Dependency] private ThievingSystem _thieving = default!;
}
