using Godot;

public partial class Player : CharacterBody3D
{
    public const float Speed = 15.0f;
    public const float AirDrag = 0.2f;
    public const float GroundFriction = 20.0f;
    public const float JumpVelocity = 4.5f;
    public const float MouseScale = 0.005f;
    
    public bool NoPulseDelay { get; set; } = true;

    [Export]
    public float PulseHeadSpeed { get; set; } = 20.0f; // per sec
    [Export]
    public float PulseTailSpeed { get; set; } = 40.0f;
    [Export]
    public float PulseTailOffset { get; set; } = -60.0f;
    
    // perhaps instead of clamping radius, have faster tail vs head speed and cancel when tail overtakes?

    private Camera3D m_fpcam;
    private Label m_debugHUD;

    private Pulse m_pulse;

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
        m_fpcam = GetNode<Camera3D>("FPCamera");
        m_debugHUD = GetNode<Label>("DebugHUD");
        m_debugHUD.Text = "";
        m_pulse = null;
        
        CaptureMouse = true;
    }

    public override void _Process(double delta)
    {
        if (m_pulse != null)
        {
#if DEBUG
            m_debugHUD.Text = $"Pulse:\n  Center   = {m_pulse.Origin}\n  Trailing = {m_pulse.TrailingRadius:N2}\n  Leading  = {m_pulse.LeadingRadius:N2}";
#endif
            if (m_pulse.Update(delta) == CompletionStatus.Completed)
            {
                m_pulse = null;
                m_debugHUD.Text = "";
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        bool isOnFloor = IsOnFloor();
        float drag = isOnFloor ? GroundFriction : AirDrag;
        velocity.X = Mathf.MoveToward(Velocity.X, 0, drag);
        velocity.Z = Mathf.MoveToward(Velocity.Z, 0, drag);

        if (isOnFloor)
        {
            Vector2 inputDir = Input.GetVector("move_right", "move_left", "move_back", "move_forward");
            Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
            if (direction != Vector3.Zero)
            {
                velocity.X = direction.X * Speed;
                velocity.Z = direction.Z * Speed;
            }
        }
        else
        {
            velocity += GetGravity() * (float)delta;
        }

        Velocity = velocity;
        if (MoveAndSlide())
        {
            for (int i = 0; i < GetSlideCollisionCount(); ++i)
            {
                var col = GetSlideCollision(i);
                if (col.GetCollider() is Node n &&
                    n.IsInGroup("Interactable"))
                {
                    n.QueueFree();
                }
            }
        }

    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMove)
        {
            if (!CaptureMouse)
            {
                return;
            }

            Quaternion *= Quaternion.FromEuler(new Vector3(0, -mouseMove.Relative.X * MouseScale, 0));

            float pitch = -mouseMove.Relative.Y * MouseScale;
            var camRot = m_fpcam.Rotation;
            camRot.X = Mathf.Clamp(camRot.X + pitch, -Mathf.Pi / 2 + 0.0001f, Mathf.Pi / 2 - 0.0001f);
            m_fpcam.Rotation = camRot; // quat?
        }

        else if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.IsActionPressed("capture_mouse"))
            {
                CaptureMouse = !CaptureMouse;
            }

            if (keyEvent.IsActionPressed("primary_action") &&
                (m_pulse == null || NoPulseDelay))
            {
                m_pulse = new Pulse(GlobalTransform.Origin, PulseTailSpeed, PulseHeadSpeed, PulseTailOffset);
            }
        }
    }

}

enum CompletionStatus
{
    InProgress,
    Completed,
}

class Pulse
{
    public Vector3 Origin { get; }
    public float TrailingRadius { get; private set; }
    public float LeadingRadius { get; private set; }

    readonly float m_tailSpeed;
    readonly float m_headSpeed;
    readonly float m_tailOffset;

    public Pulse(Vector3 center, float tailSpeed, float headSpeed, float tailOffset)
    {
        Origin = center;
        m_tailSpeed = tailSpeed;
        m_headSpeed = headSpeed;
        m_tailOffset = tailOffset;

        TrailingRadius = tailOffset;
        LeadingRadius = 0.0f;

        RenderingServer.GlobalShaderParameterSet("sense_sphere_center", Origin);
        RenderingServer.GlobalShaderParameterSet("sense_sphere_trailing_radius", TrailingRadius);
        RenderingServer.GlobalShaderParameterSet("sense_sphere_leading_radius", LeadingRadius);
    }

    public CompletionStatus Update(double delta)
    {
        TrailingRadius += (float)(m_tailSpeed * delta);
        LeadingRadius += (float)(m_headSpeed * delta);
        
        if (TrailingRadius >= LeadingRadius)
        {
            RenderingServer.GlobalShaderParameterSet("sense_sphere_trailing_radius", 0);
            RenderingServer.GlobalShaderParameterSet("sense_sphere_leading_radius", 0);
            return CompletionStatus.Completed;
        }

        RenderingServer.GlobalShaderParameterSet("sense_sphere_trailing_radius", TrailingRadius);
        RenderingServer.GlobalShaderParameterSet("sense_sphere_leading_radius", LeadingRadius);

        return CompletionStatus.InProgress;
    }
}
