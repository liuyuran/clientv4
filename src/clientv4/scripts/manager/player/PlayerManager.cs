using System;
using System.Collections.Generic;
using game.scripts.manager.reset;
using Godot;

namespace game.scripts.manager.player;

public partial class PlayerManager: IReset, IDisposable {
    public static PlayerManager instance { get; private set; } = new();
    
    private readonly Dictionary<long, PlayerInfo> _playersByPeerId = new();
    private readonly Dictionary<ulong, PlayerInfo> _playersById = new();
    private readonly HashSet<(ulong playerId, ulong worldId, Vector3I chunkPosition)> _sentChunks = [];
    private ulong _nextPlayerId = 1;
    
    /// <summary>
    /// only execute on the server or network master, need to read data from archive files
    /// </summary>
    public void RegisterPlayer(long peerId, string nickname) {
        var playerInfo = new PlayerInfo {
            peerId = peerId,
            nickname = nickname,
            playerId = _nextPlayerId++
        };
        _playersByPeerId[peerId] = playerInfo;
        _playersById[playerInfo.playerId] = playerInfo;
        GD.Print("Registered player: " + playerInfo.nickname + " with ID: " + playerInfo.playerId);
    }
    
    public PlayerInfo GetPlayerByPeerId(long peerId) {
        return _playersByPeerId.GetValueOrDefault(peerId);
    }
    
    public PlayerInfo GetPlayerById(ulong playerId) {
        return _playersById.GetValueOrDefault(playerId);
    }
    
    public IEnumerable<PlayerInfo> GetAllPlayers() {
        return _playersById.Values;
    }
    
    public bool HasSentChunk(ulong playerId, ulong worldId, Vector3I chunkPosition) {
        return _sentChunks.Contains((playerId, worldId, chunkPosition));
    }
    
    public void MarkChunkSent(ulong playerId, ulong worldId, Vector3I chunkPosition) {
        _sentChunks.Add((playerId, worldId, chunkPosition));
    }

    public void UnmarkChunkSentForAllPlayers(ulong worldId, Vector3I chunkPosition) {
        _sentChunks.RemoveWhere(x => x.worldId == worldId && x.chunkPosition == chunkPosition);
    }
    
    public void RemovePlayer(long peerId) {
        if (_playersByPeerId.TryGetValue(peerId, out var playerInfo)) {
            _playersById.Remove(playerInfo.playerId);
            _playersByPeerId.Remove(peerId);
            _sentChunks.RemoveWhere(x => x.playerId == playerInfo.playerId);
        }
    }
    
    public void UpdatePlayerPosition(long peerId, Vector3 position) {
        if (_playersByPeerId.TryGetValue(peerId, out var playerInfo)) {
            playerInfo.position = position;
        }
    }
    
    public void SetPlayerWorld(long peerId, ulong worldId) {
        if (_playersByPeerId.TryGetValue(peerId, out var playerInfo)) {
            playerInfo.worldId = worldId;
        }
    }
    
    public void UpdatePlayerPing(long peerId, uint ping) {
        if (_playersByPeerId.TryGetValue(peerId, out var playerInfo)) {
            playerInfo.ping = ping;
        }
    }
    
    public Vector3 GetPlayerPosition(long peerId) {
        return _playersByPeerId.TryGetValue(peerId, out var playerInfo) ? playerInfo.position : Vector3.Zero;
    }

    public void Reset() {
        instance = new PlayerManager();
        Dispose();
    }
    public void Dispose() {
        _playersByPeerId.Clear();
        _playersById.Clear();
        _sentChunks.Clear();
        _animationPlayer.QueueFree();
        _animationPlayer = null;
        _animationLibraries.Clear();
        GC.SuppressFinalize(this);
    }
}

public class PlayerInfo {
    public required long peerId { get; init; }
    public required ulong playerId { get; init; }
    public Vector3 position { get; set; }
    public ulong worldId { get; set; }
    public uint ping { get; set; }
    public string nickname { get; set; } = "Player";
}