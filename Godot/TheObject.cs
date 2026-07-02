using Godot;

public partial class TheObject : Node3D
{
    private Node3D m_thing;
    private Node3D m_thing2;
    private Node3D m_core;

    public override void _Ready()
    {
        m_thing = GetNode<Node3D>("thing");
        m_thing2 = GetNode<Node3D>("thing2");
        m_core = GetNode<Node3D>("core");
    }

    public override void _Process(double delta)
    {
        float t = (float)(Time.GetTicksMsec() % 628300) * 0.001f;

        // Rotate thing1 with a mostly circular motion in X/Y with a little Z wobble
        m_thing.Rotation = new Vector3(
            Mathf.Cos(t * 0.7f) * 2.3f,
            Mathf.Sin(t * 0.8f) * 1.3f,
            Mathf.Sin(t * 1.6f) * 0.8f);

        // similar to thing1 but with different frequencies/phases
        m_thing2.Rotation = new Vector3(
            Mathf.Cos(t * 1.8f + 1.3f) * 0.5f,
            Mathf.Sin(t * 2.5f) * 0.25f,
            Mathf.Cos(t * 2.1f + 0.7f) * 0.4f);

        // core pulses slightly
        float pulse = 1.0f + Mathf.Sin(t * 3.5f) * 0.2f;
        m_core.Scale = Vector3.One * pulse;
    }
}