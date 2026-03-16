using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Shitcode.Heretic.Curses;

[Serializable, NetSerializable, DataRecord]
public partial struct CurseData(NetEntity ent, string name, float multiplier, TimeSpan nextCurseTime)
{
    public NetEntity Entity = ent;

    public string Name = name;

    public float Multiplier = multiplier;

    public TimeSpan NextCurseTime = nextCurseTime;
}

[Serializable, NetSerializable]
public sealed class PickCurseVictimState(HashSet<CurseData> data) : BoundUserInterfaceState
{
    public HashSet<CurseData> Data = data;
}

[Serializable, NetSerializable]
public sealed class CurseSelectedMessage(NetEntity ent, EntProtoId curse) : BoundUserInterfaceMessage
{
    public NetEntity Victim = ent;

    public EntProtoId Curse = curse;
}

[Serializable, NetSerializable]
public enum HereticCurseUiKey : byte
{
    Key
}
