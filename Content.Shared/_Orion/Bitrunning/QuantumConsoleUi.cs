using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[Serializable, NetSerializable]
public enum QuantumConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class QuantumConsoleBoundUiState : BoundUserInterfaceState
{
    public bool Connected;
    public NetEntity? Server;
    public string? CurrentDomain;
    public int Occupants;
    public int ConnectedPods;
    public int ServerPoints;
    public int ScannerTier;
    public BitrunningServerState State;
    public bool Broadcast;
    public bool ExtremeDifficultyUnlocked;
    public float CooldownTotalSeconds;
    public float CooldownRemainingSeconds;
    public List<BitrunningDomainListing> Domains;
    public List<BitrunningOccupantListing> ConnectedAvatars;

    public QuantumConsoleBoundUiState(
        bool connected,
        NetEntity? server,
        string? currentDomain,
        int occupants,
        int connectedPods,
        int serverPoints,
        int scannerTier,
        BitrunningServerState state,
        bool broadcast,
        bool extremeDifficultyUnlocked,
        float cooldownTotalSeconds,
        float cooldownRemainingSeconds,
        List<BitrunningDomainListing> domains,
        List<BitrunningOccupantListing> connectedAvatars)
    {
        Connected = connected;
        Server = server;
        CurrentDomain = currentDomain;
        Occupants = occupants;
        ConnectedPods = connectedPods;
        ServerPoints = serverPoints;
        ScannerTier = scannerTier;
        State = state;
        Broadcast = broadcast;
        ExtremeDifficultyUnlocked = extremeDifficultyUnlocked;
        CooldownTotalSeconds = cooldownTotalSeconds;
        CooldownRemainingSeconds = cooldownRemainingSeconds;
        Domains = domains;
        ConnectedAvatars = connectedAvatars;
    }
}

[Serializable, NetSerializable]
public sealed class BitrunningDomainListing
{
    public string Id;
    public string Name;
    public string Description;
    public int Cost;
    public string Reward;
    public BitrunningDifficulty Difficulty;
    public bool IsModular;
    public bool HasSecondaryObjectives;

    public BitrunningDomainListing(string id, string name, string description, int cost, string reward, BitrunningDifficulty difficulty, bool isModular, bool hasSecondaryObjectives)
    {
        Id = id;
        Name = name;
        Description = description;
        Cost = cost;
        Reward = reward;
        Difficulty = difficulty;
        IsModular = isModular;
        HasSecondaryObjectives = hasSecondaryObjectives;
    }
}

[Serializable, NetSerializable]
public sealed class BitrunningOccupantListing
{
    public string Name;
    public bool NoHit;

    public BitrunningOccupantListing(string name, bool noHit)
    {
        Name = name;
        NoHit = noHit;
    }
}

[Serializable, NetSerializable]
public sealed class QuantumConsoleLoadDomainMessage(string domainId) : BoundUserInterfaceMessage
{
    public string DomainId = domainId;
}

[Serializable, NetSerializable]
public sealed class QuantumConsoleRandomDomainMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class QuantumConsoleStopDomainMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class QuantumConsoleRefreshMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class QuantumConsoleBroadcastMessage(bool enabled) : BoundUserInterfaceMessage
{
    public bool Enabled = enabled;
}
