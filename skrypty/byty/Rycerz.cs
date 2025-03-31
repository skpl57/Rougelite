using Godot;
using Godot.Collections;

public partial class Rycerz : CharacterBody2D
{
    // 1 - Mapa
    // 2 - Postacie
    // 3 - Damage
    // 4 - Platformy
    // 5 - Ataki

    //Zmienne
    public bool animacjaNaAtak = false; // true - animacja trwa
    private bool ruch = false; // false - atak z góry, true - atak z dołu
    public bool rolling = false;

    //Obiekty
    public AnimatedSprite2D animacje;
    public Node2D mieczoPodobne;
    private Area2D hitboxyMiecza;
    private AnimationPlayer ataki;

    private Area2D hurtBox;
    public Timer czasNiesmiertelnosci;
    public Timer znikanie;


    //Klasy
    private AnimacjePostaci animacjePostaci;
    private Ruch_Rycerz ruch_Rycerz;
    private Zdrowie zdrowie;
    public override void _Ready()
    {
        // Pobieranie elementów
        animacje = GetNode<AnimatedSprite2D>("Animacje");

        mieczoPodobne = GetNode<Node2D>("Mieczopodobne");
        hitboxyMiecza = GetNode<Area2D>("Mieczopodobne/ZakresMiecza");
        ataki = GetNode<AnimationPlayer>("Mieczopodobne/Ataki");

        czasNiesmiertelnosci = GetNode<Timer>("Obrazenia/CzasNiesmiertelnosci");
        hurtBox = GetNode<Area2D>("Obrazenia/HurtBox");
        znikanie = GetNode<Timer>("Obrazenia/Zanikanie");

        // Klasy
        animacjePostaci = new AnimacjePostaci(this, ataki);
        ruch_Rycerz = new Ruch_Rycerz(this);
        zdrowie = new Zdrowie(this);

        // Zakończenie animacji
        ataki.AnimationFinished += animacjePostaci.KoniecAnimacji;
        animacje.AnimationFinished += animacjePostaci.KoniecPrzewerotu;

        // Obrażenia
        hurtBox.BodyEntered += zdrowie.wejscieBody;
        hurtBox.BodyExited += zdrowie.wyjscieBody;
        czasNiesmiertelnosci.Timeout += zdrowie.koniecCzasu;

        znikanie.Timeout += zdrowie.zanikanieNiesmiertelnosci;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;
        Vector2 direction = Input.GetVector("left", "right", "up", "down");

        // Obsługa animacji i ataku
        animacjePostaci.akcje(direction, velocity);
        animacjePostaci.atak(ref ruch, rolling);

        // Obsługa ruchu
        ruch_Rycerz.przesuwanieSie(direction, ref velocity);
        ruch_Rycerz.grawitacja(ref velocity, delta);

        Velocity = velocity;
        MoveAndSlide();
    }
}

public partial class AnimacjePostaci : Node
{
    private Rycerz rycerz;
    private AnimationPlayer ataki;
    private Node2D Upadek;
    private Vector2 zatrzymanieWpowietrzu;

    private bool atakWDol = false; //Ten taki atak spadający
    private bool skonczonyAtak = false; // zakończenie animacji tego ruchu miecza przy spadaniu
    public AnimacjePostaci(Rycerz rycerz, AnimationPlayer ataki)
    {
        //Konstruktor
        this.rycerz = rycerz;
        this.ataki = ataki;
        Upadek = rycerz.GetNode<Node2D>("Mieczopodobne/Ataki/Upadek");

    }

    public void akcje(Vector2 direction, Vector2 velocity)
    {
        //Animacje Postaci
        if(rycerz.rolling){return;}

        if (rycerz.IsOnFloor())
        {
            if (direction.X == 0.0)
            {
                rycerz.animacje.Play("idle");
            }
            else
            {
                rycerz.animacje.Play("run");
            }
        }
        else
        {
            if (velocity.Y < 0)
            {
                rycerz.animacje.Play("jump");
            }
            else
            {
                rycerz.animacje.Play("falling");
            }
        }
    }

    public void atak(ref bool ruch, bool rolling)
    {
        //Akcje które może wykonać
        if(rolling){
            return;
        }
        if (Input.IsActionJustPressed("atak") && !rycerz.animacjaNaAtak )
        {
            if(Input.IsActionPressed("down") && !rycerz.IsOnFloor() && !atakWDol){
                zatrzymanieWpowietrzu = rycerz.Position;
                if(!ruch)
                {
                    ataki.Play("atakNaDolZGory");
                }
                else
                {
                    ataki.Play("atakNaDolZDolu");
                }
                ruch = true;
                atakWDol = true;
            }
            else if (!ruch){
                ataki.Play("atakZGory");
                ruch = !ruch;
            }
            else
            {
                ataki.Play("atakZDolu");
                ruch = !ruch;
            }
            rycerz.animacjaNaAtak = true;
        }
        else if(rycerz.animacjaNaAtak && atakWDol){
            //Stanie w miejscu
            rycerz.Position = new Vector2(zatrzymanieWpowietrzu.X, zatrzymanieWpowietrzu.Y + 1);
            zatrzymanieWpowietrzu.Y -= 0.2f;
        }
        else if(skonczonyAtak && rycerz.IsOnFloor()){
            //Lądowanie
            ataki.Play("Ladowanie");
            skonczonyAtak = false;
            Upadek.Position = new Vector2(rycerz.Position.X, rycerz.Position.Y - 3);
        }
    }

    public void KoniecAnimacji(StringName animName)
    {
        if(animName.ToString().StartsWith("atakNaDol")){
            atakWDol = false;
            skonczonyAtak = true;
            return;
        }
        rycerz.animacjaNaAtak = false;
    }
    public void KoniecPrzewerotu(){
        if(rycerz.animacje.Animation == "roll")
        {
            rycerz.rolling = false;
        }
    }
}

public partial class Ruch_Rycerz:Node
{
    private Rycerz rycerz;
    private const float Speed = 150.0f;
    private const float JumpVelocity = -350.0f;
    private int kierunek = 0; // Przechwycenie direction

    public Ruch_Rycerz(Rycerz rycerz)
    {
        this.rycerz = rycerz;
    }

    public void grawitacja(ref Vector2 velocity, double delta)
    {
        if (!rycerz.IsOnFloor())
        {
            velocity += rycerz.GetGravity() * (float)delta;
        }
        if (Input.IsActionJustPressed("ui_accept") && rycerz.IsOnFloor() && !rycerz.rolling)
        {
            velocity.Y = JumpVelocity;
        }
    }

    public void przesuwanieSie(Vector2 direction, ref Vector2 velocity)
    {
        if(rycerz.rolling){
            velocity.X = kierunek * Speed * 1.2f;
            return;
        }

        if(Input.IsActionJustPressed("dodge") && !rycerz.animacjaNaAtak){
            rycerz.animacje.Play("roll");
            rycerz.rolling = true;
            if(rycerz.animacje.FlipH == true){
                kierunek = -1;
            }
            else{
                kierunek = 1;
            }
        }
        
        if (direction != Vector2.Zero)
        {
            //Ustawia kierunek obrotu i prędkość
            if(!rycerz.animacjaNaAtak){
                if(direction.X < 0){
                    rycerz.mieczoPodobne.Scale = new Vector2(-1, 1);
                }
                else{
                    rycerz.mieczoPodobne.Scale = new Vector2(1, 1);
                }
                rycerz.animacje.FlipH = direction.X < 0;
            }
            velocity.X = direction.X * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
        }
    }
}



public partial class Zdrowie : Node{
    private Rycerz rycerz;
    private AnimatedSprite2D zycie;
    private Area2D hurtBox;
    public Array lista = new Array();
    
    private int healthPoints = 3;
    private bool czasTrwa = false;
    private int licznik = 0;
    public Zdrowie(Rycerz rycerz){
        this.rycerz = rycerz;
        zycie = rycerz.GetNode<AnimatedSprite2D>("CanvasLayer/Control/HBoxContainer/Container/zycie");
    }
    public void wejscieBody(Node body){
        if(body.IsInGroup("wrogowie")){
            lista.Add(body.Name);
            otrzymywanieObrazen();
        }
    }
    public void wyjscieBody(Node body){
        lista.Remove(body.Name);
    }
    private void otrzymywanieObrazen(){
        if(healthPoints != 0 && !czasTrwa){
            healthPoints--;
            czasTrwa = true;
            zycie.Play(healthPoints.ToString());
            rycerz.czasNiesmiertelnosci.Start();
            rycerz.znikanie.Start();
            rycerz.animacje.SelfModulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        }
    }
    public void koniecCzasu(){ 
        czasTrwa = false;
        rycerz.znikanie.Stop();
        licznik = 0;
        rycerz.animacje.SelfModulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        if(lista.Count > 0){
            otrzymywanieObrazen();
        }
    }

    public void zanikanieNiesmiertelnosci(){
        licznik++;
        rycerz.znikanie.Start();
        if(licznik%3 == 0){
            rycerz.animacje.SelfModulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        }
        else{
            rycerz.animacje.SelfModulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        }
    }

}
