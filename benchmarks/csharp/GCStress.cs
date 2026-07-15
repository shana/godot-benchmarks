using Godot;
using System;
using Godot.Collections;

public partial class GCStress : Benchmark
{
    private SphereMesh CircleMesh = new SphereMesh();


    public GCStress()
    {
        benchmark_time = 3e7;
        test_idle = true;
    }

    public Godot.RigidBody2D CreateCircle(bool visualize)
    {
        Godot.RigidBody2D rigid_body = new Godot.RigidBody2D();
        CollisionShape2D collision_shape = new CollisionShape2D();
        rigid_body.ContinuousCd = Godot.RigidBody2D.CcdMode.Disabled;

        if (visualize)
        {
            rigid_body.AddChild(new MeshInstance2D { Mesh = CircleMesh });
        }

        collision_shape.Shape = new CircleShape2D();
        rigid_body.AddChild(collision_shape);

        return rigid_body;
    }


    public Node2D BenchmarkGCStress1()
    {
        var scene = new GCStressScene(true, CreateCircle, 2_000, true);
        return scene;
    }
}


public partial class GCStressScene : Node2D
{
    private readonly bool visualize;
    private readonly Func<bool, Godot.RigidBody2D> drawFunc;
    private readonly int amount;
    private readonly bool drawEveryFrame;
    Array<Godot.RigidBody2D> list;

    const double SPREAD_H = 1600.0f;
    const double SPREAD_V = 800.0f;

    public GCStressScene(bool visualize, Func<bool, Godot.RigidBody2D> drawFunc, int amount, bool drawEveryFrame) : base()
    {
        this.visualize = visualize;
        this.drawFunc = drawFunc;
        this.amount = amount;
        this.drawEveryFrame = drawEveryFrame;
        list = new();
    }

    public override void _Ready()
    {
        if (visualize)
        {
            Camera2D camera = new Camera2D {Position = new(0.0f, -100.0f), Zoom = new(0.5f, 0.5f)};
            AddChild(camera);
        }

        SetProcess(drawEveryFrame);
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        GD.Print("Existing GCStress scene");

        foreach (var body in list)
        {
            RemoveChild(body);
        }
    }

    public override void _Draw()
    {
        foreach (var body in list)
        {
            RemoveChild(body);
        }

        list.Clear();

        for (int i = 0; i < amount; i++)
        {
            Godot.RigidBody2D body = drawFunc(visualize);
            body.Position = new((float)GD.RandRange(-SPREAD_H, SPREAD_H), (float)GD.RandRange(0.0d, -SPREAD_V));
            AddChild(body);
            list.Add(body);
        }
    }
}
