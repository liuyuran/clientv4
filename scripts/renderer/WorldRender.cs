﻿using Godot;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using game.scripts.config;
using game.scripts.manager;
using game.scripts.utils;
using Godot.Collections;

namespace game.scripts.renderer;

/// <summary>
/// chunk manage node but only include block data
/// </summary>
public partial class WorldRender : Node3D {
    private readonly System.Collections.Generic.Dictionary<(ulong, Vector3I), ChunkRenderItem> _loadedChunks = new();

    private IEnumerable<(ulong, Vector3I)> GetLoadedChunkCoordinates() {
        return _loadedChunks.Keys;
    }

    private static IEnumerable<(ulong, Vector3I)> GetRequiredChunkCoordinates(ulong worldId, Vector3 playerPosition) {
        var centerChunk = playerPosition.ToChunkPosition();
        for (var x = centerChunk.X - Config.ChunkRenderDistance; x <= centerChunk.X + Config.ChunkRenderDistance; x++)
        for (var y = centerChunk.Y - Config.ChunkRenderDistance; y <= centerChunk.Y + Config.ChunkRenderDistance; y++)
        for (var z = centerChunk.Z - Config.ChunkRenderDistance; z <= centerChunk.Z + Config.ChunkRenderDistance; z++)
            yield return (worldId, new Vector3I(x, y, z));
    }

    public override void _Ready() {
        ResourcePackManager.instance.ScanResourcePacks();
        MaterialManager.instance.GenerateMaterials();
        MapManager.instance.OnBlockChanged += OnInstanceOnOnBlockChanged;
    }

    private void OnInstanceOnOnBlockChanged(ulong worldId, Vector3 position, ulong blockId, Direction direction) {
        var chunkCoord = position.ToChunkPosition();
        var localPosition = position.ToLocalPosition();
        if (_loadedChunks.TryGetValue((worldId, chunkCoord), out var chunk)) {
            chunk.Rpc(ChunkRenderItem.MethodName.SetBlock, localPosition, blockId, (int) direction);
        }
    }

    public override void _ExitTree() {
        MapManager.instance.OnBlockChanged -= OnInstanceOnOnBlockChanged;
        foreach (var chunk in _loadedChunks.Values) {
            chunk.QueueFree();
        }

        _loadedChunks.Clear();
    }

    public override void _Process(double delta) {
        var loadedChunks = GetLoadedChunkCoordinates().ToHashSet();
        // query local player
        var requiredChunks = new HashSet<(ulong, Vector3I)>();
        // if master client or dedicated server, load all player's chunk
        if (PlatformUtil.isNetworkMaster) {
            var players = PlayerManager.instance.GetAllPlayers();
            foreach (var playerInfo in players) {
                var position = PlayerManager.instance.GetPlayerPosition(playerInfo.peerId);
                using var iter = GetRequiredChunkCoordinates(playerInfo.worldId, position).GetEnumerator();
                while (iter.MoveNext()) {
                    var chunkCoord = iter.Current;
                    requiredChunks.Add(chunkCoord);
                }
            }
        } else {
            var currentPeerId = Multiplayer.MultiplayerPeer.GetUniqueId();
            var players = PlayerManager.instance.GetAllPlayers();
            foreach (var playerInfo in players) {
                if (playerInfo.peerId != currentPeerId) continue; // only load current player
                var position = PlayerManager.instance.GetPlayerPosition(playerInfo.peerId);
                using var iter = GetRequiredChunkCoordinates(playerInfo.worldId, position).GetEnumerator();
                while (iter.MoveNext()) {
                    var chunkCoord = iter.Current;
                    requiredChunks.Add(chunkCoord);
                }
            }
        }

        // load can be load, if not data, wait next tick
        foreach (var chunkCoord in requiredChunks.Except(loadedChunks)) {
            var chunk = new ChunkRenderItem();
            var data = GetBlockData(chunkCoord.Item1, chunkCoord.Item2);
            if (data == null) continue;
            chunk.InitData(chunkCoord.Item2, data);
            AddChild(chunk);
            chunk.Position = new Vector3(
                chunkCoord.Item2.X * Config.ChunkSize,
                chunkCoord.Item2.Y * Config.ChunkSize,
                chunkCoord.Item2.Z * Config.ChunkSize
            );
            _loadedChunks[chunkCoord] = chunk;
        }

        // unload chunks that are no longer required
        foreach (var chunkCoord in loadedChunks.Except(requiredChunks)) {
            if (!_loadedChunks.TryGetValue(chunkCoord, out var chunk)) continue;
            chunk.QueueFree();
            _loadedChunks.Remove(chunkCoord);
            if (PlatformUtil.isNetworkMaster) PlayerManager.instance.UnmarkChunkSentForAllPlayers(chunkCoord.Item1, chunkCoord.Item2);
        }

        // if not network master, don't send data. GodotObject's performance is not good
        if (PlatformUtil.isNetworkMaster) {
            var players = PlayerManager.instance.GetAllPlayers();
            foreach (var playerInfo in players) {
                // if is current player, skip
                if (playerInfo.peerId == Multiplayer.MultiplayerPeer.GetUniqueId()) continue;
                var position = PlayerManager.instance.GetPlayerPosition(playerInfo.peerId);
                var chunkPosition = position.ToChunkPosition();
                var data = MapManager.instance.GetBlockData(0, chunkPosition);
                if (data == null) continue;
                if (PlayerManager.instance.HasSentChunk(playerInfo.playerId, playerInfo.worldId, chunkPosition)) continue;
                PlayerManager.instance.MarkChunkSent(playerInfo.playerId, playerInfo.worldId, chunkPosition);
                var blocks = new Array<ulong>();
                var directions = new Array<Direction>();
                for (var x = 0; x < Config.ChunkSize; x++) {
                    for (var y = 0; y < Config.ChunkSize; y++) {
                        for (var z = 0; z < Config.ChunkSize; z++) {
                            var blockData = data[x][y][z];
                            blocks.Add(blockData.BlockId);
                            directions.Add(blockData.Direction);
                        }
                    }
                }

                Rpc(MethodName.ReceiveChunkData, playerInfo.worldId, chunkPosition, blocks, directions);
            }
        }
    }

    private static BlockData[][][] GetBlockData(ulong worldId, Vector3I chunkPosition) {
        return MapManager.instance.GetBlockData(worldId, chunkPosition, PlatformUtil.isNetworkMaster);
    }

    /// <summary>
    /// receive data from the network master or dedicated server
    /// </summary>
    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    private void ReceiveChunkData(ulong worldId, Vector3I chunkPosition, Array<ulong> blocks, Array<Direction> directions) {
        if (blocks == null || blocks.Count == 0) {
            GD.PrintErr($"Received empty chunk data for {chunkPosition}");
            return;
        }

        var data = new BlockData[Config.ChunkSize][][];
        for (var x = 0; x < Config.ChunkSize; x++) {
            data[x] = new BlockData[Config.ChunkSize][];
            for (var y = 0; y < Config.ChunkSize; y++) {
                data[x][y] = new BlockData[Config.ChunkSize];
                for (var z = 0; z < Config.ChunkSize; z++) {
                    data[x][y][z] = new BlockData {
                        BlockId = blocks[x * Config.ChunkSize * Config.ChunkSize + y * Config.ChunkSize + z],
                        Direction = directions[x * Config.ChunkSize * Config.ChunkSize + y * Config.ChunkSize + z]
                    };
                }
            }
        }

        MapManager.instance.OverwriteBlockData(worldId, chunkPosition, data);
    }
}