using Content.Shared.Body;
using Content.Shared.Objectives;
using Content.Shared.Roles;
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
    /// Prototypes of target entity.
    /// Used for ui (sprite) or direct check for stealing
    /// </summary>
    public List<EntProtoId>? Protos;

    public ProtoId<SpyBountyPrototype> BountyProto;

    public SpriteSpecifier? Sprite;

    public string Name = string.Empty;

    public string Description = string.Empty;

    // Either ListingPrototype or SpyBountyPrototype
    public string Reward = string.Empty;

    public bool Equals(SpyBounty? other)
    {
        if (other is null)
            return false;
        return ReferenceEquals(this, other) || BountyProto.Equals(other.BountyProto);
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

    [DataField]
    public bool Repeatable;
}

[Prototype]
public sealed partial class SpyRewardPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpyBountyDifficulty Difficulty;

    [DataField(required: true)]
    public List<ProtoId<ListingPrototype>> RewardSelection = new();

    [DataField]
    public LocId? RewardNameOverride;

    [DataField]
    public LocId? RewardDescriptionOverride;

    [DataField]
    public float Weight = 1f;

    [DataField]
    public float? RemoveFromPoolChanceOverride;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseSpyBountySelectorEvent : EntityEventArgs
{
    public ProtoId<SpyBountyPrototype> Id;

    public ProtoId<SpyRewardPrototype> Reward;

    public abstract BaseSpyBountySelectorEvent GetEvent();

    public object Initialize(
        ProtoId<SpyBountyPrototype> id,
        ProtoId<SpyRewardPrototype> reward)
    {
        Id = id;
        Reward = reward;
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
    public List<EntProtoId> Protos;

    public override BaseSpyBountySelectorEvent GetEvent()
    {
        return new SpyPrototypeBountySelectorEvent { Protos = new(Protos) };
    }
}

public sealed partial class SpySpecificEntityBountySelectorEvent : BaseSpyBountySelectorEvent
{
    [DataField(required: true)]
    public List<EntProtoId> Protos;

    [DataField(required: true)]
    public string QueryComp;

    [DataField]
    public List<EntProtoId>? Areas;

    public override BaseSpyBountySelectorEvent GetEvent()
    {
        return new SpySpecificEntityBountySelectorEvent
        {
            Protos = new(Protos),
            QueryComp = QueryComp,
            Areas = Areas is not { } areas ? null : new(areas)
        };
    }
}

public sealed partial class SpyOrganBountySelectorEvent : BaseSpyBountySelectorEvent
{
    [DataField(required: true)]
    public HashSet<ProtoId<OrganCategoryPrototype>> ValidOrgans;

    [DataField]
    public HashSet<ProtoId<DepartmentPrototype>>? DepartmentWhitelist;

    [DataField]
    public HashSet<ProtoId<DepartmentPrototype>>? DepartmentBlacklist;

    public override BaseSpyBountySelectorEvent GetEvent()
    {
        return new SpyOrganBountySelectorEvent
        {
            ValidOrgans = new(ValidOrgans),
            DepartmentWhitelist = DepartmentWhitelist == null ? null : new(DepartmentWhitelist),
            DepartmentBlacklist = DepartmentBlacklist == null ? null : new(DepartmentBlacklist)
        };
    }
}
