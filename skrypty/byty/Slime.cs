using System;
using Godot;

public partial class Slime : CharacterBody2D
{
	private Ruch_Slime ruch_Slime;
	private Animacje_Slime animacje_Slime;
	private Zdrowie_Slime zdrowie_Slime;
	public RayCast2D UpperRay;
	public RayCast2D DiagonalRay;
	public RayCast2D wallRay;
	public RayCast2D floorRay;
	public Timer cooldownSkoku;
	public Area2D damage;
	public bool dead = false;
    public override void _Ready()
    {
		ruch_Slime = new Ruch_Slime(this);
		animacje_Slime = new Animacje_Slime(this);
		zdrowie_Slime = new Zdrowie_Slime(this);

		DiagonalRay = GetNode<RayCast2D>("RayCasty/DiagonalRay");
		UpperRay = GetNode<RayCast2D>("RayCasty/UpperRay");
	
		wallRay = GetNode<RayCast2D>("RayCasty/WallRay");
		floorRay = GetNode<RayCast2D>("RayCasty/FloorRay");

		cooldownSkoku = GetNode<Timer>("cooldownSkoku");
		cooldownSkoku.Timeout += ruch_Slime.koniecCzasu;

		damage = GetNode<Area2D>("Damage");
		damage.AreaEntered += zdrowie_Slime.wejscieObiektu;
	}


    public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
	
		ruch_Slime.grawitacja(ref velocity, delta);
		velocity.X = ruch_Slime.przesuwanieSie(velocity, delta);
		
		animacje_Slime.zmianaAnimacji(velocity);

		Velocity = velocity;
		MoveAndSlide();
	}
}
public partial class  Ruch_Slime:Node{
	private Slime slime;
	private bool upadek = false;
	private bool probaSkoku = false;
	private bool wPowietrzu = false;
	private const float JumpVelocity = -400.0f;
	public const float Speed = 125.0f;
	private int direction = 1;
	private Vector2 scale;
	private Random random = new Random();
	public Ruch_Slime(Slime slime){
		this.slime = slime;
	}
	public void grawitacja(ref Vector2 velocity, double delta){
		if (!slime.IsOnFloor())
		{
			velocity.Y += slime.GetGravity().Y * (float)delta;
		}
		if(!slime.UpperRay.IsColliding() && slime.DiagonalRay.IsColliding() && !probaSkoku){
			int liczba = random.Next(1, 101);
			if(liczba <= 75){
				wPowietrzu = true;
				velocity.Y = JumpVelocity; 
			}
			probaSkoku = true;	
			slime.cooldownSkoku.Start();
		}
		
	}
	
	public float przesuwanieSie(Vector2 velocity, double delta){
		if(!wPowietrzu){
			if(slime.wallRay.IsColliding()){
				direction *= -1;
				slime.Scale = new Vector2(slime.Scale.X * -1, slime.Scale.Y);
			}
			if(slime.floorRay.IsColliding()){
				upadek = false;
			}
			if(!slime.floorRay.IsColliding() && !upadek){
				int liczba = random.Next(1, 101);
				if(liczba%2 == 0){
					direction *= -1;
					slime.Scale = new Vector2(slime.Scale.X * -1, slime.Scale.Y);
				}
				else{
					upadek = true;
				}
			}
		}
		return velocity.X = Speed * direction * (float) delta * 20;
	}
	public void koniecCzasu(){
		probaSkoku = false;
		wPowietrzu = false;
	}
}


public partial class Animacje_Slime : Node{
	private Slime slime;
	private AnimatedSprite2D animacje;
	public Animacje_Slime(Slime slime){
		this.slime = slime;
		animacje = slime.GetNode<AnimatedSprite2D>("Animacje");
	}
	public void zmianaAnimacji(Vector2 velocity){
		if(velocity.X != 0 && velocity.Y == 0){
			animacje.Play("run");
		}
		else if(velocity.Y < 0){
			animacje.Play("jump");
		}
	}
}
public partial class Zdrowie_Slime : Node{
	private Slime slime;
	private int healthPoints;
	private CpuParticles2D particle;
	public Zdrowie_Slime(Slime slime){
		this.slime = slime;
		healthPoints = 25;
		particle = slime.GetNode<CpuParticles2D>("Particle");
	}

	public void umieranie(){
		
	}
	public void wejscieObiektu(Node area){
		if(area.Name == "ZakresMiecza"){
			particle.Emitting = false;
			particle.Emitting = true;
			healthPoints -= Global.obrazenia;
			if(healthPoints <= 0){
				slime.QueueFree();
			}
		}
	}
}