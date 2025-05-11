using System;
using Godot;

namespace game.scripts;

public partial class EntityRender : Node
{
	private MultiMesh _blockSpawner;
	
	public override void _Ready() {
		_blockSpawner = GetNode<MultiMeshInstance3D>("BlockSpawner").Multimesh;
		Console.WriteLine(_blockSpawner.InstanceCount);
	}

	public override void _Process(double delta) {
		
	}
}
