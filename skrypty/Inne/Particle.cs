using Godot;
public partial class Particle : CpuParticles2D
{
    public override void _Ready()
    {
        Emitting = true;
    }
    public override void _Process(double delta)
    {
        if(Emitting == false){
            QueueFree();
        }
    }
}
