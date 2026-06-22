using Content.Shared.Objectives;
using Content.Shared.Store;

namespace Content.Trauma.Shared.Spy;

[Serializable, NetSerializable, DataRecord]
public sealed partial class SpyBounty : IEquatable<SpyBounty>
{
    /// <summary>
    /// Whether the bounty was completed by some spy
    /// </summary>
    public bool Claimed;

    /// <summary>
    /// Specific entity that needs to be stolen
    /// If empty, use proto check instead
    /// </summary>
    public List<NetEntity> ValidEntities = new();

    /// <summary>
    /// Prototype of target entity.
    /// Used for ui (sprite) or direct check for stealing
    /// </summary>
    public EntProtoId? Proto;

    public ProtoId<SpyBountyPrototype> BountyProto;

    public SpriteSpecifier? Sprite;

    public SpyBountyDifficulty Difficulty;

    public string Name = string.Empty;

    public string Description = string.Empty;

    public ProtoId<ListingPrototype> Reward;

    public TimeSpan TheftTime;

    public bool Equals(SpyBounty? other)
    {
        if (other is null)
            return false;
        return ReferenceEquals(this, other) || BountyProto.Equals(other.Reward);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is SpyBounty other && Equals(other);
    }

    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return BountyProto.GetHashCode();
    }
}

[Serializable, NetSerializable]
public enum SpyBountyDifficulty : byte
{
    Easy,
    Medium,
    Hard,
}

[Prototype]
public sealed partial class SpyBountyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpyBountyDifficulty Difficulty;

    [DataField(required: true, serverOnly: true)]
    public BaseSpyBountySelectorEvent Selector = default!;

    [DataField]
    public TimeSpan TheftTime = TimeSpan.FromSeconds(2);
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseSpyBountySelectorEvent : EntityEventArgs
{
    public ProtoId<ListingPrototype> Reward;

    public SpyBountyDifficulty Difficulty;

    public TimeSpan TheftTime;

    public ProtoId<SpyBountyPrototype> Id;

    public abstract BaseSpyBountySelectorEvent GetEvent();

    public object Initialize(ProtoId<ListingPrototype> reward,
        SpyBountyDifficulty difficulty,
        TimeSpan theftTime,
        ProtoId<SpyBountyPrototype> id)
    {
        Reward = reward;
        Difficulty = difficulty;
        TheftTime = theftTime;
        Id = id;
        return this;
    }
}

public sealed partial class SpyStealTargetBountySelectorEvent : BaseSpyBountySelectorEvent
{
    [DataField(required: true)]
    public ProtoId<StealTargetGroupPrototype> StealTarget;

    public override BaseSpyBountySelectorEvent GetEvent()
    {
        return new SpyStealTargetBountySelectorEvent { StealTarget = StealTarget };
    }
}

public sealed partial class SpyPrototypeBountySelectorEvent : BaseSpyBountySelectorEvent
{
    [DataField(required: true)]
    public EntProtoId Proto;

    public override BaseSpyBountySelectorEvent GetEvent()
    {
        return new SpyPrototypeBountySelectorEvent { Proto = Proto };
    }
}

public sealed partial class SpySpecificEntityBountySelectorEvent : BaseSpyBountySelectorEvent
{
    [DataField(required: true)]
    public EntProtoId Proto;

    [DataField(required: true)]
    public string QueryComp;

    [DataField]
    public List<EntProtoId>? Areas;

    public override BaseSpyBountySelectorEvent GetEvent()
    {
        return new SpySpecificEntityBountySelectorEvent
        {
            Proto = Proto, QueryComp = QueryComp, Areas = Areas is not { } areas ? null : new(areas)
        };
    }
}
