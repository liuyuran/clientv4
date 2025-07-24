using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using game.scripts.manager;
using game.scripts.manager.map;
using game.scripts.manager.player;
using game.scripts.renderer;
using game.scripts.server.ECSBridge.gravity;
using game.scripts.server.ECSBridge.input;
using Godot;

namespace game.scripts.server.ECSBridge.sync;

public static class PlayerRenderUtil {
    private static Node LoadModel(string path) {
        var gltfDocumentLoad = new GltfDocument();
        var gltfStateLoad = new GltfState();
        var error = gltfDocumentLoad.AppendFromFile(path, gltfStateLoad);
        if (error == Error.Ok) return gltfDocumentLoad.GenerateScene(gltfStateLoad);
        GD.PrintErr($"Couldn't load glTF scene (error code: {error}).");
        return null;
    }
    
    public static void CreatePlayer(bool mainPlayer, Entity entity, PackedScene playerPrototype, Node root, Dictionary<Entity, Node3D> entityNodes) {
        var worldContainerNode = root.GetNode<WorldContainer>("worlds");
        var worldNode = worldContainerNode.GetCurrentSubViewport();
        var player = playerPrototype.Instantiate<CharacterBody3D>();
        player.Name = $"Player_{entity.Id}";
        if (mainPlayer) {
            worldNode.AddChild(player);
            entityNodes[entity] = player;
            entity.GetComponent<CPhysicsVelocity>().Rid = player.GetRid();
            var spaceState = player.GetWorld3D().DirectSpaceState;
            entity.GetComponent<CCamera>().SpaceState = spaceState;
            var camera = player.GetNode<Camera3D>("head/eyes");
            entity.GetComponent<CCamera>().Camera = camera;
            camera.Current = true;
        } else {
            var camera = player.GetNode<Camera3D>("head/eyes");
            camera.QueueFree();
            worldNode.AddChild(player);
            entityNodes[entity] = player;
            entity.GetComponent<CPhysicsVelocity>().Rid = player.GetRid();
        }
        player.GlobalPosition = new Vector3(0, MapManager.instance.GetNearestLand(0, Vector3.Zero) + 3, 0);

        var node = LoadModel(ResourcePackManager.instance.GetFileAbsolutePath("core:/models/player.glb"));
        if (node == null) throw new Exception("Failed to load player model.");
        player.AddChild(node);
        node.Name = "PlayerModel";
        PlayerManager.instance.AttachAnimationNode(player);
    }

    public static void UpdatePlayer(Entity entity, Node node) {
        //
    }
    
    public static void SyncPlayerMoveStatus(Node node, Entity entity, double delta, ref CPhysicsVelocity velocity) {
        if (node is not CharacterBody3D body3D) return;
        if (body3D.IsOnFloor() && velocity.Velocity.Y < 0) velocity.Velocity.Y = 0;
        body3D.Velocity = velocity.Velocity * (float)delta;
        body3D.MoveAndSlide();
        var animationPlayer = body3D.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animationPlayer != null) {
            if (body3D.IsOnFloor()) {
                // loop play
                if (animationPlayer.CurrentAnimation != "core/run" && animationPlayer.CurrentAnimation != "core/idle") {
                    animationPlayer.Stop();
                }
                if (animationPlayer.HasAnimation("core/run") && animationPlayer.HasAnimation("core/idle")) {
                    animationPlayer.Play(velocity.Velocity.Length() > 0.1f ? "core/run" : "core/idle");
                }
            } else {
                if (animationPlayer.CurrentAnimation != "core/jump") {
                    animationPlayer.Stop();
                }
                if (animationPlayer.HasAnimation("core/jump")) {
                    animationPlayer.Play("core/jump");
                }
            }
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
}