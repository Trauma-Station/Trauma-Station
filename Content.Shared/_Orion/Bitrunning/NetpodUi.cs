using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[Serializable, NetSerializable]
public enum NetpodUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class NetpodLoadoutEntry(string id, string name)
{
    public string Id = id;
    public string Name = name;
}

[Serializable, NetSerializable]
public sealed class NetpodBoundUiState(string? selectedLoadout, List<NetpodLoadoutEntry> loadouts) : BoundUserInterfaceState
{
    public string? SelectedLoadout = selectedLoadout;
    public List<NetpodLoadoutEntry> Loadouts = loadouts;
}

[Serializable, NetSerializable]
public sealed class NetpodSelectLoadoutMessage(string loadoutId) : BoundUserInterfaceMessage
{
    public string LoadoutId = loadoutId;
}
