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
	public bool uderzony = false;
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
		velocity = ruch_Slime.przesuwanieSie(velocity, delta);
		
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
	private bool spadaPoUderzeniu = false;
	private const float JumpVelocity = -400.0f;
	public const float Speed = 125.0f;
	private int direction = 1;

	private Vector2 scale;
	private Random random = new Random();


	private RayCast2D leftRay;
	private RayCast2D rightRay;
	public Ruch_Slime(Slime slime){
		this.slime = slime;
		direction = (int) slime.Scale.X;
		leftRay = slime.GetNode<RayCast2D>("RayCasty/LeftRay");
		rightRay = slime.GetNode<RayCast2D>("RayCasty/RightRay");
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
	
	public Vector2 przesuwanieSie(Vector2 velocity, double delta){
		if(slime.uderzony){
			int kierunek = 1;
			slime.uderzony = false;

			wPowietrzu = true;
			probaSkoku = true;
			spadaPoUderzeniu = true;

			if(leftRay.IsColliding() && leftRay.GetCollider().ToString().Contains("CharacterBody2D")){
				if(slime.Scale != new Vector2(1,1)){
					kierunek = -1;
				}
				else{
					kierunek = 1;
				}
			}
			else if(rightRay.IsColliding() && rightRay.GetCollider().ToString().Contains("CharacterBody2D")){
				if(slime.Scale != new Vector2(1,1)){
					kierunek = 1;
				}
				else{
					kierunek = -1;
				}
			}
			return new Vector2(Speed * kierunek * (float) delta * 30, -150);
		}
		
		if(slime.IsOnFloor() && spadaPoUderzeniu){
			probaSkoku = false;
			wPowietrzu = false;
			spadaPoUderzeniu = false;
		}
		
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
		if(!spadaPoUderzeniu){
			return new Vector2(Speed * direction * (float) delta * 20, velocity.Y);
		}
		else{
			return new Vector2(velocity.X, velocity.Y);
		}
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
	private PackedScene particleSmierci_Paczka = GD.Load<PackedScene>("res://entity/particle.tscn");
	public Zdrowie_Slime(Slime slime){
		this.slime = slime;
		healthPoints = 25;
	}

	public void wejscieObiektu(Node area){
		if(area.Name == "ZakresMiecza"){
			healthPoints -= Global.obrazenia;
			umieranie();
			slime.uderzony = true;
			if(healthPoints <= 0){
				slime.QueueFree();
			}
		}
	}
	public void umieranie(){
		CpuParticles2D particleSmierci = (CpuParticles2D)particleSmierci_Paczka.Instantiate();
		particleSmierci.Position = slime.Position - new Vector2(0,8);
		slime.GetParent().AddChild(particleSmierci);
	}
}