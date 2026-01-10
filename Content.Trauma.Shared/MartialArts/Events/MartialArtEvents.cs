using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.MartialArts.Events;

[Serializable, NetSerializable]
public sealed class MartialArtsSaying(LocId saying) : EntityEventArgs
{
    public LocId Saying = saying;
}
