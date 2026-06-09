using Godot;

public partial class Player : CharacterBody3D
{
	public const float Speed = 15.0f;
	public const float JumpVelocity = 4.5f;
	public const float MouseScale = 0.005f;

	private Camera3D m_fpcam;

	// todo: elsewhere
	public bool CaptureMouse
	{
		get => m_captureMouse;
		set
		{
			m_captureMouse = value;
			Input.MouseMode = value ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }
	}
	private bool m_captureMouse;
    
	public override void _Ready()
    {
		m_fpcam = GetNode<Camera3D>("fpcam");
		CaptureMouse = true;
    }

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		Vector2 inputDir = Input.GetVector("move_right", "move_left", "move_back", "move_forward");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
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

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMove)
		{
			Quaternion *= Quaternion.FromEuler(new Vector3(0, -mouseMove.Relative.X * MouseScale, 0));

			float pitch = -mouseMove.Relative.Y * MouseScale;
			var camRot = m_fpcam.Rotation;
			camRot.X = Mathf.Clamp(camRot.X + pitch, -Mathf.Pi / 2 + 0.0001f, Mathf.Pi / 2 - 0.0001f);
			m_fpcam.Rotation = camRot; // quat?
        }
	}
}
