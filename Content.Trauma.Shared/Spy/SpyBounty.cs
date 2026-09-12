// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Objectives;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Trauma.Shared.Spy;

/// <summary>
/// A bounty for stealing certain entity and collecting the reward for spy antag
/// </summary>
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

/// <summary>
/// Defines a bounty for spy to steal
/// </summary>
[Prototype]
public sealed partial class SpyBountyPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SpyBountyPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public SpyBountyDifficulty Difficulty;

    /// <summary>
    /// Spies with this job deparntment won't be able to claim this bounty
    /// </summary>
    [DataField]
    public HashSet<ProtoId<DepartmentPrototype>>? DepartmentBlacklist;

    /// <summary>
    /// Spies with this job won't be able to claim this bounty
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>>? JobBlacklist;

    /// <summary>
    /// Event that controls creation of this bounty
    /// </summary>
    [DataField(required: true, serverOnly: true)]
    public BaseSpyBountySelectorEvent Selector = default!;

    /// <summary>
    /// How long does scanning target takes
    /// </summary>
    [DataField]
    public TimeSpan TheftTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// If true, the bounty may appear again in future bounty refreshes after being claimed
    /// </summary>
    [DataField]
    public bool Repeatable;
}

/// <summary>
/// Reward that spy can get from bounties
/// Usually store listings are used as a reward but this can be too if you need custom weight, name/desc or being able
/// to give spy a selection of listings to choose from when collecting reward
/// </summary>
[Prototype]
public sealed partial class SpyRewardPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpyBountyDifficulty Difficulty;

    /// <summary>
    /// Spy will be able to select from these listings when collecting reward
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<ListingPrototype>> RewardSelection = new();

    /// <summary>
    /// Overrides name for reward in ui
    /// if null, first listing in RewardSelection will be used
    /// </summary>
    [DataField]
    public LocId? RewardNameOverride;

    /// <summary>
    /// Overrides desc for reward in ui
    /// if null, first listing in RewardSelection will be used
    /// </summary>
    [DataField]
    public LocId? RewardDescriptionOverride;

    /// <summary>
    /// More weight - more likely for reward to appear
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Bounty rewards have a chance to never appear based on difficulty
    /// This overrides the default value
    /// <see cref="SpyRuleComponent.ChancesToRemoveRewardFromPool"/>
    /// </summary>
    [DataField]
    public float? RemoveFromPoolChanceOverride;
}

/// <summary>
/// Raised on gamerule, used to create actual bounty and put it in bounty list
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseSpyBountySelectorEvent : EntityEventArgs
{
    public ProtoId<SpyBountyPrototype> Id;

    public string Reward = string.Empty;

    public abstract BaseSpyBountySelectorEvent GetEvent();

    public object Initialize(
        ProtoId<SpyBountyPrototype> id,
        string reward)
    {
        Id = id;
        Reward = reward;
        return this;
    }
}

/// <summary>
/// Selects target based on steal group and verifies map existence
/// </summary>
public sealed partial class SpyStealTargetBountySelectorEvent : BaseSpyBountySelectorEvent
{
    [DataField(required: true)]
    public ProtoId<StealTargetGroupPrototype> StealTarget;

    public override BaseSpyBountySelectorEvent GetEvent()
    {
        return new SpyStealTargetBountySelectorEvent { StealTarget = StealTarget };
    }
}

/// <summary>
/// Selects target that matches the prototype, doesn't verify map existence
/// </summary>
public sealed partial class SpyPrototypeBountySelectorEvent : BaseSpyBountySelectorEvent
{
    [DataField(required: true)]
    public List<EntProtoId> Protos;

    public override BaseSpyBountySelectorEvent GetEvent()
    {
        return new SpyPrototypeBountySelectorEvent { Protos = new(Protos) };
    }
}

/// <summary>
/// Queries for a specific target on map, checks its proto and area it is located in, if it matches, makes it valid for theft
/// </summary>
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

/// <summary>
/// Queries for living people with job that matches department blacklist/whilteist and selects organ to steal from them
/// </summary>
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
