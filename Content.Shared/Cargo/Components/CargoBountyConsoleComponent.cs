using System;
using System.Collections.Generic;
using Content.Shared.Cargo;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Cargo.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CargoBountyConsoleComponent : Component
{
    [DataField("bountyLabelId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BountyLabelId = "PaperCargoBountyManifest";

    [DataField("nextPrintTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPrintTime = TimeSpan.Zero;

    [DataField("printDelay")]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);

    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [DataField("skipSound")]
    public SoundSpecifier SkipSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField("denySound")]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_two.ogg");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDenySoundTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan DenySoundDelay = TimeSpan.FromSeconds(2);
}

[NetSerializable, Serializable]
public sealed class CargoBountyConsoleState : BoundUserInterfaceState
{
    public List<CargoBountyData> Bounties;
    public List<CargoBountyHistoryData> History;
    public TimeSpan UntilNextSkip;

    public float MilitaryReputation;
    public float MedicalReputation;
    public float ServiceReputation;

    public CargoBountyConsoleState(
        List<CargoBountyData> bounties,
        List<CargoBountyHistoryData> history,
        TimeSpan untilNextSkip)
    {
        Bounties = bounties;
        History = history;
        UntilNextSkip = untilNextSkip;

        MilitaryReputation = 0f;
        MedicalReputation = 0f;
        ServiceReputation = 0f;
    }

    public CargoBountyConsoleState(
        List<CargoBountyData> bounties,
        List<CargoBountyHistoryData> history,
        TimeSpan untilNextSkip,
        float militaryReputation,
        float medicalReputation,
        float serviceReputation)
    {
        Bounties = bounties;
        History = history;
        UntilNextSkip = untilNextSkip;

        MilitaryReputation = militaryReputation;
        MedicalReputation = medicalReputation;
        ServiceReputation = serviceReputation;
    }
}

[Serializable, NetSerializable]
public sealed class BountyPrintLabelMessage : BoundUserInterfaceMessage
{
    public string BountyId;

    public BountyPrintLabelMessage(string bountyId)
    {
        BountyId = bountyId;
    }
}

[Serializable, NetSerializable]
public sealed class BountySkipMessage : BoundUserInterfaceMessage
{
    public string BountyId;

    public BountySkipMessage(string bountyId)
    {
        BountyId = bountyId;
    }
}
