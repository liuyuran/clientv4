using System.Collections.Generic;
using Godot;

namespace game.scripts.renderer;

/// <summary>
/// generate the extra mesh for digging block
/// </summary>
public partial class DigShadowInstance3D : MultiMeshInstance3D {
    private readonly string _shaderCode = ResourceLoader.Load<string>("res://shader/dig-shadow.gdshader");
    private readonly Dictionary<Vector3I, float> _diggingBlocks = new();

    public override void _Ready() {
        var multiMesh = new MultiMesh();
        multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        multiMesh.UseCustomData = true;
        multiMesh.InstanceCount = 0;
        multiMesh.Mesh = new PlaneMesh { Size = new Vector2(1.0f, 1.0f), SubdivideWidth = 0, SubdivideDepth = 0 };
        Multimesh = multiMesh;
        var crackShader = new Shader();
        crackShader.Code = _shaderCode;
        var crackMaterial = new ShaderMaterial();
        crackMaterial.Shader = crackShader;
        crackMaterial.Set("shader_param/crack_color", new Vector3(0.0f, 0.0f, 0.0f));
        crackMaterial.Set("shader_param/crack_thickness", 0.02f);
        crackMaterial.Set("shader_param/noise_scale", 10.0f);
        MaterialOverride = crackMaterial;
    }

    public void SetDigProgress(Vector3I pos, float progress) {
        _diggingBlocks[pos] = progress;
        if (progress is >= 1.0f or <= 0) {
            _diggingBlocks.Remove(pos);
        }
        UpdateAllDigStatus();
    }

    private void UpdateAllDigStatus() {
        if (Multimesh.InstanceCount != _diggingBlocks.Count) {
            Multimesh.InstanceCount = _diggingBlocks.Count;
        }

        var i = 0;
        foreach (var (pos, progress) in _diggingBlocks) {
            Multimesh.SetInstanceCustomData(i, new Color(progress, pos.X, pos.Y, pos.Z));
            var transform = new Transform3D {
                Origin = pos
            };
            Multimesh.SetInstanceTransform(i, transform);;
            i++;
        }
    }
}