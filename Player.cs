using Godot;
using System;

[GlobalClass]
public partial class Player : CharacterBody3D
{
	private const float Speed = 5.0f;
	private const float JumpVelocity = 4.5f;
	[Export] public Camera3D Camera;
	[Export] public CollisionShape3D CollisionShape;
	[Export] public MeshInstance3D Mesh;
	[Export] public float MouseSensitivity = 0.002f;

	private float _cameraRotationX = 0f;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			
			_cameraRotationX -= mouseMotion.Relative.Y * MouseSensitivity;
			_cameraRotationX = Mathf.Clamp(_cameraRotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
			
			Vector3 rotation = Camera.Rotation;
			rotation.X = _cameraRotationX;
			Camera.Rotation = rotation;
		}

		if (Input.IsActionJustPressed("ui_cancel"))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
				Input.MouseMode = Input.MouseModeEnum.Visible;
			else
				Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		var velocity = Velocity;
		
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}
		
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		var inputDir = Input.GetVector("strafe_left", "strafe_right", "strafe_forward", "strafe_back");
		var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
