using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.config;
using game.scripts.manager;
using game.scripts.manager.map;
using game.scripts.manager.player;
using game.scripts.server.ECSBridge.block;
using game.scripts.server.ECSBridge.gravity;
using game.scripts.server.ECSBridge.input;
using game.scripts.server.ECSBridge.render;
using game.scripts.server.ECSBridge.sync;
using game.scripts.utils;
using Godot;
using ModLoader.config;

namespace game.scripts.server.ECSBridge;

/// <summary>
/// the bridge node for the Friflo ECS system.
/// </summary>
public partial class ECSSystemBridge: Node {
    private EntityStore _world;
    private SystemRoot _systemRoot;
    private readonly Dictionary<Entity, Node3D> _entityNodes = new();
    private bool _isInitialized;
    [Export] private PackedScene _playerPrototype;

    public override void _Ready() {
        _world = new EntityStore();
        _world.OnEntityCreate += WorldOnOnEntityCreate;
        _world.OnEntityDelete += WorldOnOnEntityDelete;
        _world.OnComponentAdded += WorldOnOnComponentAdded;
        _world.OnComponentRemoved += WorldOnOnComponentRemoved;
        _systemRoot = new SystemRoot(_world) {
            new SMoveSystem(_world),
            new SJumpAndGravity(_world),
            new SBlockDestroyOrPlace()
        };
    }
    
    private IEnumerable<Vector3I> GetRequiredChunkCoordinates(Vector3 playerPosition) {
        var centerChunk = playerPosition.ToChunkPosition();
        for (var x = centerChunk.X - Config.ChunkRenderDistance - 1; x <= centerChunk.X + Config.ChunkRenderDistance + 1; x++)
        for (var y = centerChunk.Y - Config.ChunkRenderDistance - 1; y <= centerChunk.Y + Config.ChunkRenderDistance + 1; y++)
        for (var z = centerChunk.Z - Config.ChunkRenderDistance - 1; z <= centerChunk.Z + Config.ChunkRenderDistance + 1; z++)
            yield return new Vector3I(x, y, z);
    }

    public override void _Process(double delta) {
        delta = 0.016;
        if (!_isInitialized) {
            var currentPlayerId = Multiplayer.MultiplayerPeer.GetUniqueId();
            var playerInfo = PlayerManager.instance.GetPlayerByPeerId(currentPlayerId);
            if (playerInfo == null) return;
            using var iter = GetRequiredChunkCoordinates(playerInfo.position).GetEnumerator();
            while (iter.MoveNext()) {
                var chunkCoord = iter.Current;
                MapManager.instance.GetBlockData(playerInfo.worldId, chunkCoord, true, false);
            }
            _isInitialized = true;
            var player = _world.CreateEntity(new CRenderType {
                Type = ERenderType.MainPlayer
            }, new CInputEvent(), new CTransform(), new CCamera(), new CPhysicsVelocity(), new CPeer {
                PeerId = Multiplayer.GetUniqueId()
            }, new CPhysicsStatus {
                Jumping = false
            }, new CJumpStatus());
            player.AddSignalHandler<SignalBlockChanged>(signal => {
                MapManager.instance.SetBlock(signal.Event.WorldId, signal.Event.Position, signal.Event.BlockId, signal.Event.Direction);
            });
            GetTree().Root.AddChild(new PlayerControl(player));
            GameStatus.SetStatus(GameStatus.Status.Playing);
            return;
        }

        // 先往ECS同步一遍位置和旋转信息
        foreach (var (entity, node) in _entityNodes) {
            entity.GetComponent<CTransform>().Position = node.GlobalPosition;
            entity.GetComponent<CTransform>().Rotation = node.GlobalRotation;
            entity.GetComponent<CTransform>().BasisX = node.GlobalTransform.Basis.X;
            entity.GetComponent<CTransform>().BasisY = node.GlobalTransform.Basis.Y;
            entity.GetComponent<CTransform>().BasisZ = node.GlobalTransform.Basis.Z;
            if (node is CharacterBody3D body3D && entity.HasComponent<CPhysicsStatus>()) {
                entity.GetComponent<CPhysicsStatus>().Jumping = !body3D.IsOnFloor();
            }
        }

        _systemRoot.Update(new UpdateTick((float)delta, (float)((double)Time.GetTicksMsec() / 1000)));
        // 更新人物位置和旋转
        var commandBuffer = _world.GetCommandBuffer();
        _world.Query<CPhysicsVelocity>().AnyTags(Tags.Get<THasDirtData>()).ForEachEntity((ref CPhysicsVelocity velocity, Entity entity) => {
            if (_entityNodes.TryGetValue(entity, out var node)) {
                PlayerRenderUtil.SyncPlayerMoveStatus(node, entity, delta, ref velocity);
            }

            commandBuffer.RemoveTag<THasDirtData>(entity.Id);
        });
        commandBuffer.Playback();
    }

    public override void _ExitTree() {
        _world.OnEntityCreate -= WorldOnOnEntityCreate;
        _world.OnEntityDelete -= WorldOnOnEntityDelete;
        _world.OnComponentAdded -= WorldOnOnComponentAdded;
        _world.OnComponentRemoved -= WorldOnOnComponentRemoved;
        foreach (var node in _entityNodes.Values) {
            if (node is CharacterBody3D player) {
                PlayerManager.instance.DetachAnimationNode(player);
                player.QueueFree();
            } else {
                node.QueueFree();
            }
        }
        _entityNodes.Clear();
    }
    
    private void CreateNodeByComponents(Entity entity) {
        if (entity.HasComponent<CRenderType>()) {
            var renderType = entity.GetComponent<CRenderType>();
            switch (renderType.Type) {
                case ERenderType.MainPlayer: {
                    PlayerRenderUtil.CreatePlayer(true, entity, _playerPrototype, GetParent(), _entityNodes);
                    break;
                }
                case ERenderType.Player: {
                    PlayerRenderUtil.CreatePlayer(false, entity, _playerPrototype, GetParent(), _entityNodes);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// remove component callback
    /// </summary>
    private void WorldOnOnComponentRemoved(ComponentChanged obj) {
        if (!_entityNodes.TryGetValue(obj.Entity, out var node)) return;
        if (!obj.Entity.HasComponent<CRenderType>()) {
            if (node is CharacterBody3D player) {
                PlayerManager.instance.DetachAnimationNode(player);
                player.QueueFree();
            } else {
                node.QueueFree();
            }
            _entityNodes.Remove(obj.Entity);
            return;
        }
        PlayerRenderUtil.UpdatePlayer(obj.Entity, node);
    }

    /// <summary>
    /// add or replace component callback
    /// </summary>
    private void WorldOnOnComponentAdded(ComponentChanged obj) {
        if (!obj.Entity.HasComponent<CRenderType>()) return;
        if (_entityNodes.TryGetValue(obj.Entity, out var node)) {
            PlayerRenderUtil.UpdatePlayer(obj.Entity, node);
        } else {
            CreateNodeByComponents(obj.Entity);
        }
    }

    /// <summary>
    /// delete entity callback
    /// </summary>
    private void WorldOnOnEntityDelete(EntityDelete obj) {
        if (!_entityNodes.TryGetValue(obj.Entity, out var node)) return;
        if (node is CharacterBody3D player) {
            PlayerManager.instance.DetachAnimationNode(player);
            player.QueueFree();
        } else {
            node.QueueFree();
        }
        _entityNodes.Remove(obj.Entity);
    }

    /// <summary>
    /// create entity callback
    /// </summary>
    private void WorldOnOnEntityCreate(EntityCreate obj) {
        if (!obj.Entity.HasComponent<CRenderType>()) return;
        if (_entityNodes.ContainsKey(obj.Entity)) return;
        CreateNodeByComponents(obj.Entity);
    }
}