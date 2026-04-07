// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Antag;

/// <summary>
/// Lets security summon a random antag if all the antags died to prevent chudshift.
/// Also gives them free money to have IC reason to do it...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AntagSummonerComponent : Component
{
    /// <summary>
    /// The gamerule to start when used.
    /// </summary>
    [DataField]
    public EntProtoId GameRule = "SecurityRandomAntagRule";

    /// <summary>
    /// How much money to give <see cref="Account"/>.
    /// </summary>
    [DataField]
    public int Reward = 10000;

    /// <summary>
    /// The cargo account to give money to.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> Account = "Security";

    /// <summary>
    /// When another antag can next be summoned.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextSummon;

    /// <summary>
    /// The initial delay for summoning item after this item is spawned.
    /// </summary>
    [DataField]
    public TimeSpan InitialCooldown = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long you have to wait to summon another antag.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(30);
}

[Serializable, NetSerializable]
public enum AntagSummonerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SummonAntagMessage : BoundUserInterfaceMessage;
