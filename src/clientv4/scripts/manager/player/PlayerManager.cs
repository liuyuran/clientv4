using System;
using System.Collections.Generic;
using System.Text.Json;
using game.scripts.manager.archive;
using game.scripts.manager.reset;
using game.scripts.utils;
using generated.archive;
using Godot;
using Google.FlatBuffers;
using ModLoader.archive;

namespace game.scripts.manager.player;

public partial class PlayerManager : IReset, IArchive, IDisposable {
    public static PlayerManager instance { get; private set; } = new();

    private const string ArchiveFilename = "players.dat";
    private const string PlayerArchiveFilename = "player-{0}.json";
    private readonly Dictionary<long, PlayerInfo> _playersByPeerId = new();
    private readonly Dictionary<ulong, PlayerInfo> _playersById = new();
    private readonly HashSet<(ulong playerId, ulong worldId, Vector3I chunkPosition)> _sentChunks = [];
    private ulong _nextPlayerId = 1;

    /// <summary>
    /// only execute on the server or network master, need to read data from archive files
    /// </summary>
    public void RegisterPlayer(long peerId, string uuid, string nickname) {
        var playerInfo = new PlayerInfo {
            uuid = uuid,
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
        if (_animationPlayer != null) {
            _animationPlayer.QueueFree();
            _animationPlayer = null;
        }

        _animationLibraries.Clear();
        GC.SuppressFinalize(this);
    }

    private PlayerInfo TryLoadArchive(string uuid, string nickname, long peerId) {
        var filename = string.Format(PlayerArchiveFilename, uuid);
        var bytes = ArchiveManager.instance.GetFileAsBytesFromCurrentArchive(filename);
        if (bytes == null || bytes.Length == 0) {
            GD.Print($"No player archive found for {uuid}, creating new player.");
            return new PlayerInfo {
                uuid = uuid,
                nickname = nickname,
                peerId = peerId,
                playerId = _nextPlayerId++
            };
        }
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var jsonItem = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        if (jsonItem == null) {
            GD.PrintErr($"Failed to deserialize player archive for {uuid}, creating new player.");
            return new PlayerInfo {
                uuid = uuid,
                nickname = nickname,
                peerId = peerId,
                playerId = _nextPlayerId++
            };
        }
        var playerInfo = new PlayerInfo {
            uuid = uuid,
            nickname = nickname,
            peerId = peerId,
            playerId = _nextPlayerId++
        };
        if (jsonItem.TryGetValue("position", out var positionObj) && positionObj is string positionStr) {
            if (Vector3.Zero.TryParse(positionStr, out var position)) {
                playerInfo.position = position;
            } else {
                GD.PrintErr($"Failed to parse position for player {uuid}, using default position.");
                playerInfo.position = Vector3.Zero;
            }
        } else {
            GD.PrintErr($"No position found for player {uuid}, using default position.");
            playerInfo.position = Vector3.Zero;
        }

        if (jsonItem.TryGetValue("worldId", out var worldIdStr) && worldIdStr is ulong worldId) {
            playerInfo.worldId = worldId;
        } else {
            playerInfo.worldId = 0;
        }

        return playerInfo;
    }

    public void Archive(Dictionary<string, byte[]> fileList) {
        var fbb = new FlatBufferBuilder(1024);
        var offset = PlayerManagerMeta.CreatePlayerManagerMeta(fbb, _nextPlayerId);
        fbb.Finish(offset.Value);
        fileList.Add(ArchiveFilename, fbb.SizedByteArray());
        foreach (var player in _playersById.Values) {
            var jsonItem = new Dictionary<string, object> {
                { "position", player.position.ToArchiveString() },
                { "worldId", player.worldId },
            };
            var json = JsonSerializer.Serialize(jsonItem);
            var playerData = System.Text.Encoding.UTF8.GetBytes(json);
            fileList.Add(string.Format(PlayerArchiveFilename, player.uuid), playerData);
        }
    }

    public void Recover(Func<string, byte[]> getDataFunc) {
        var data = getDataFunc(ArchiveFilename);
        if (data == null || data.Length == 0) {
            GD.Print("No player data found, starting fresh.");
            return;
        }

        var playerManagerMeta = PlayerManagerMeta.GetRootAsPlayerManagerMeta(new ByteBuffer(data));
        _nextPlayerId = playerManagerMeta.IdBreakpoint;
    }
}

public class PlayerInfo {
    public required long peerId { get; init; }
    public required string uuid { get; init; }
    public required ulong playerId { get; init; }
    public Vector3 position { get; set; }
    public ulong worldId { get; set; }
    public uint ping { get; set; }
    public string nickname { get; set; } = "Player";
}