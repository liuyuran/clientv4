using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using game.scripts.manager;
using game.scripts.server.ECSBridge.block;
using game.scripts.server.ECSBridge.gravity;
using game.scripts.server.ECSBridge.input;
using game.scripts.server.ECSBridge.render;
using game.scripts.server.ECSBridge.sync;
using game.scripts.utils;
using Godot;

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
            new SJump(_world),
            new SBlockDestroyOrPlace()
        };
    }

    public override void _PhysicsProcess(double delta) {
        const float gravity = -9.8f; // 设置重力加速度
        foreach (var (entity, node) in _entityNodes) {
            if (node is not CharacterBody3D body3D) continue;
            // 更新玩家的物理状态
            if (body3D.IsOnFloor()) continue;
            body3D.Velocity += new Vector3(0, gravity * delta, 0);
            body3D.MoveAndSlide();
        }
    }

    public override void _Process(double delta) {
        if (!_isInitialized) {
            _isInitialized = true;
            var inputHandler = _world.CreateEntity(new CRenderType {
                Type = ERenderType.MainPlayer
            }, new CInputEvent(), new CTransform(), new CCamera(), new CPhysicsVelocity(), new CPeer {
                PeerId = Multiplayer.GetUniqueId()
            }, new CPhysicsStatus {
                Jumping = false
            });
            inputHandler.AddSignalHandler<SignalBlockChanged>(signal => {
                MapManager.instance.SetBlock(signal.Event.WorldId, signal.Event.Position, signal.Event.BlockId, signal.Event.Direction);
            });
            GetTree().Root.AddChild(new PlayerControl(inputHandler));
        }

        // 先往ECS同步一遍位置和旋转信息
        foreach (var (entity, node) in _entityNodes) {
            entity.GetComponent<CTransform>().Position = node.GlobalPosition;
            entity.GetComponent<CTransform>().Rotation = node.GlobalRotation;
            entity.GetComponent<CTransform>().BasisX = node.GlobalTransform.Basis.X;
            entity.GetComponent<CTransform>().BasisY = node.GlobalTransform.Basis.Y;
            entity.GetComponent<CTransform>().BasisZ = node.GlobalTransform.Basis.Z;
        }

        _systemRoot.Update(new UpdateTick((float)delta, (float)((double)Time.GetTicksMsec() / 1000)));
        // 更新人物位置和旋转
        var commandBuffer = _world.GetCommandBuffer();
        _world.Query<CPhysicsVelocity>().AnyTags(Tags.Get<THasDirtData>()).ForEachEntity((ref CPhysicsVelocity velocity, Entity entity) => {
            if (_entityNodes.TryGetValue(entity, out var node)) {
                if (node is not CharacterBody3D body3D) return;
                var previousPosition = body3D.GlobalPosition;
                body3D.Velocity = velocity.Velocity * (float)delta;
                body3D.MoveAndSlide();
                if (entity.HasComponent<CPhysicsStatus>()) {
                    // 如果Y轴位置发生变化，说明是跳跃或下落
                    var isJumping = Math.Abs(previousPosition.Y - body3D.GlobalPosition.Y) > 0.01f;
                    entity.GetComponent<CPhysicsStatus>().Jumping = isJumping;
                }
                if (velocity.Rotation.X != 0) {
                    body3D.RotateY(velocity.Rotation.X);
                }

                if (velocity.Rotation.Y != 0) {
                    body3D.GetNode<Node3D>("head").RotateX(velocity.Rotation.Y);
                }

                if (entity.HasComponent<CPeer>()) {
                    var peer = entity.GetComponent<CPeer>();
                    PlayerManager.instance.UpdatePlayerPosition(peer.PeerId, body3D.GlobalPosition);
                }
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
        foreach (var pair in _entityNodes) {
            var node = pair.Value;
            GetTree().Root.RemoveChild(node);
            if (node is CharacterBody3D player) {
                player.QueueFree();
            } else {
                node.QueueFree();
            }
        }
        _entityNodes.Clear();
    }

    /// <summary>
    /// add or remove node by entity components.
    /// </summary>
    /// <param name="node">godot node instance</param>
    /// <param name="entity">ecs entity reference</param>
    private void UpdateNodeByComponents(Node node, Entity entity) {
        // TODO change component
    }
    
    private void CreateNodeByComponents(Entity entity) {
        if (entity.HasComponent<CRenderType>()) {
            var renderType = entity.GetComponent<CRenderType>();
            switch (renderType.Type) {
                case ERenderType.MainPlayer: {
                    var player = _playerPrototype.Instantiate<CharacterBody3D>();
                    player.Name = $"Player_{entity.Id}";
                    GetTree().Root.AddChild(player);
                    _entityNodes[entity] = player;
                    entity.GetComponent<CPhysicsVelocity>().Rid = player.GetRid();
                    var spaceState = player.GetWorld3D().DirectSpaceState;
                    entity.GetComponent<CCamera>().SpaceState = spaceState;
                    var camera = player.GetNode<Camera3D>("head/eyes");
                    entity.GetComponent<CCamera>().Camera = camera;
                    camera.Current = true;
                    player.GlobalPosition = new Vector3(0, 3, 0);
                    break;
                }
                case ERenderType.Player: {
                    var player = _playerPrototype.Instantiate<CharacterBody3D>();
                    player.Name = $"Player_{entity.Id}";
                    var camera = player.GetNode<Camera3D>("head/eyes");
                    player.GetNode<Node3D>("head").RemoveChild(camera);
                    GetTree().Root.AddChild(player);
                    _entityNodes[entity] = player;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// remove component callback
    /// </summary>
    private void WorldOnOnComponentRemoved(ComponentChanged obj) {
        if (!_entityNodes.TryGetValue(obj.Entity, out var node)) return;
        if (!obj.Entity.HasComponent<CRenderType>()) {
            GetTree().Root.RemoveChild(node);
            if (node is CharacterBody3D player) {
                player.QueueFree();
            } else {
                node.QueueFree();
            }
            _entityNodes.Remove(obj.Entity);
            return;
        }
        UpdateNodeByComponents(node, obj.Entity);
    }

    /// <summary>
    /// add or replace component callback
    /// </summary>
    private void WorldOnOnComponentAdded(ComponentChanged obj) {
        if (!obj.Entity.HasComponent<CRenderType>()) return;
        if (_entityNodes.TryGetValue(obj.Entity, out var node)) {
            UpdateNodeByComponents(node, obj.Entity);
        } else {
            CreateNodeByComponents(obj.Entity);
        }
    }

    /// <summary>
    /// delete entity callback
    /// </summary>
    private void WorldOnOnEntityDelete(EntityDelete obj) {
        if (!_entityNodes.TryGetValue(obj.Entity, out var node)) return;
        GetTree().Root.RemoveChild(node);
        if (node is CharacterBody3D player) {
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