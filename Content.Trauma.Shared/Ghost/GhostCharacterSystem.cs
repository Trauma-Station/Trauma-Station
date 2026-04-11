// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Ghost;

/// <summary>
/// Stores characters you have played in this round, and which character you want to use for reinforcement ghost roles.
/// </summary>
public sealed class GhostCharacterSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    [ViewVariables(VVAccess.ReadWrite)] // use vvwrite probably
    private Dictionary<NetUserId, CharacterData> _data = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeNetworkEvent<GhostCharacterSetSlotMessage>(OnSetSlot);
        SubscribeNetworkEvent<GhostCharacterLoadDataMessage>(OnLoadData);
        SubscribeAllEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        AddSpawnedCharacter(args.Player.UserId, args.Profile.Name);
        SendData(args.Player); // update it for the client to know
    }

    private void OnSetSlot(GhostCharacterSetSlotMessage ev, EntitySessionEventArgs args)
    {
        SetGhostRoleSlot(args.SenderSession.UserId, ev.Slot);
    }

    private void OnLoadData(GhostCharacterLoadDataMessage args)
    {
        if (_player.LocalSession?.UserId is not {} user)
            return;

        _data[user] = args.Data;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _data.Clear();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        // new clients will have _data be empty so make them load it
        if (args.NewStatus == SessionStatus.InGame)
            SendData(args.Session);
    }

    /// <summary>
    /// Sends a player's character data to it via network message.
    /// </summary>
    public void SendData(ICommonSession session)
    {
        if (!_data.TryGetValue(session.UserId, out var data))
            return;

        var ev = new GhostCharacterLoadDataMessage(data);
        RaiseNetworkEvent(ev, session);
    }

    /// <summary>
    /// Returns the client's local data.
    /// </summary>
    public CharacterData? GetLocalData()
    {
        if (_player.LocalSession?.UserId is not {} user)
            return null;

        return _data.TryGetValue(user, out var data)
            ? data
            : null;
    }

    /// <summary>
    /// Gets the slot index for a player's desired ghost role profile.
    /// It may be out of bounds.
    /// It may have been spawned already, must be checked against HasCharacterSpawned after.
    /// </summary>
    public int? GetGhostRoleSlot(NetUserId user)
        => _data.TryGetValue(user, out var data)
            ? data.DesiredSlot
            : null;

    /// <summary>
    /// Returns true if a player has spawned as a character with a given name before.
    /// </summary>
    public bool HasCharacterSpawned(NetUserId user, string name)
        => _data.TryGetValue(user, out var data)
            ? data.SpawnedNames.Contains(name)
            : false;

    /// <summary>
    /// Sets a user's desired character slot for ghost roles.
    /// </summary>
    public void SetGhostRoleSlot(NetUserId user, int? slot)
    {
        EnsureData(user).DesiredSlot = slot;
    }

    /// <summary>
    /// Sets the client's desired character slot for ghost roles.
    /// </summary>
    public void SetGhostRoleSlot(int? slot)
    {
        if (_player.LocalSession?.UserId is not {} user)
            return;

        SetGhostRoleSlot(user, slot);
        var ev = new GhostCharacterSetSlotMessage(slot);
        RaiseNetworkEvent(ev);
    }

    /// <summary>
    /// Stores a spawned character name for a player.
    /// </summary>
    public void AddSpawnedCharacter(NetUserId user, string name)
    {
        EnsureData(user).SpawnedNames.Add(name);
    }

    private CharacterData EnsureData(NetUserId user)
    {
        if (!_data.TryGetValue(user, out var data))
            _data[user] = data = new();
        return data;
    }
}

[Serializable, NetSerializable]
public sealed class CharacterData
{
    /// <summary>
    /// The name of every character a player has spawned.
    /// Used to prevent taking the same character twice.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> SpawnedNames = new();

    /// <summary>
    /// The character slot the player wants to use for reinforcement ghost roles.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? DesiredSlot;
}

/// <summary>
/// Message sent by the client to the server to set the desired character slot for ghost roles.
/// </summary>
[Serializable, NetSerializable]
public sealed class GhostCharacterSetSlotMessage(int? slot) : EntityEventArgs
{
    public readonly int? Slot = slot;
}

/// <summary>
/// Message sent by the server to a client to update its local character data.
/// </summary>
[Serializable, NetSerializable]
public sealed class GhostCharacterLoadDataMessage(CharacterData data) : EntityEventArgs
{
    public readonly CharacterData Data = data;
}
