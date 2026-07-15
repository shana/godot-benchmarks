using Godot;
using System;

public partial class GCStress : Benchmark
{
    private SphereMesh CircleMesh = new SphereMesh();


    public GCStress()
    {
        benchmark_time = 1e7;
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
        var scene = new GCStressScene(false, CreateCircle, 10_000_000, true);
        return scene;
    }
}


public partial class GCStressScene : Node2D
{
    private readonly bool visualize;
    private readonly Func<bool, Godot.RigidBody2D> drawFunc;
    private readonly int amount;
    private readonly bool drawEveryFrame;
    //Godot.Collections.Array<LargeGC> list;
    Godot.Collections.Array<string> list;
    private ulong frameCount;
    private ulong elapsed;
    private ulong firstFrameTime;
    private ulong averageDrawTime;
    private ulong peakDrawTime;
    private ulong peakMemory;
    private ulong averageMemory;

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
        list.Clear();

        GD.Print($"GC average usage: {averageMemory / (1024 * 1024)}Mb ({averageMemory}). GC peak: {peakMemory / (1024 * 1024)}Mb ({peakMemory}). Average Draw() time: {averageDrawTime}ms. Peak Draw() time: {peakDrawTime}ms");
    }

    public override void _Draw()
    {
        GC.Collect();
        GC.Collect();

        frameCount++;
        if (firstFrameTime == 0)
        {
            firstFrameTime = Time.GetTicksMsec();
        }


        //foreach (var body in list)
        //{
        //    //if (visualize)
        //    //{
        //    //    RemoveChild(body);
        //    //}

        //    //body.QueueFree();
        //    body.Free();
        //}

        list.Clear();


        //var first = new LargeGC();
        var first = "";
        list.Add(first);

        for (int i = 1; i < amount; i++)
        {
            //Godot.RigidBody2D body = drawFunc(visualize);
            //body.Position = new((float)GD.RandRange(-SPREAD_H, SPREAD_H), (float)GD.RandRange(0.0d, -SPREAD_V));
            //if (visualize)
            //{
            //    AddChild(body);
            //}

            //var body = new LargeGC();
            var body = "";
            list.Add(body);
        }

        var start = Time.GetTicksMsec();
        // force a call to Count
        var f = list.IndexOf(first);

        // force calls to Count
        foreach (var c in list)
        {
        }

        var stop = Time.GetTicksMsec();
        elapsed += stop - start;

        ulong totalMemory = (ulong)GC.GetTotalMemory(false);
        peakMemory = Math.Max(totalMemory, peakMemory);
        if (frameCount == 1)
        {
            averageMemory = totalMemory;
        }
        else
        {
            averageMemory = averageMemory * (frameCount - 1) / frameCount + totalMemory / frameCount;
        }

        averageDrawTime = elapsed / frameCount;
        peakDrawTime = Math.Max(stop - start, peakDrawTime);

        if (stop - firstFrameTime >= 1000)
        {
            GD.Print($"GC usage: {totalMemory / (1024 * 1024)}Mb ({totalMemory}), average {averageMemory / (1024 * 1024)}Mb ({averageMemory}). Total frames:{frameCount} draw time: {stop - start}ms. average: {averageDrawTime}ms. peak: {peakDrawTime}ms");
            firstFrameTime = Time.GetTicksMsec();
        }
    }

    internal partial class LargeGC : RefCounted
    {
        //const int SIZE = 10625; //85 KB
        const int SIZE = 1; //8b

        public double[] d;
        public SmallGC m_pSmall;

        public LargeGC()
        {
            d = new double[SIZE];
            m_pSmall = null;
        }

        public virtual void AttachSmallObjects(SmallGC small)
        {
            m_pSmall = small;
        }
    }

    internal class SmallGC
    {
        public LargeGC m_pLarge;
        public SmallGC(int HasLargeObj)
        {
            if (HasLargeObj == 1)
                m_pLarge = new LargeGC();
            else
                m_pLarge = null;
        }
        public virtual void AttachSmallObjects()
        {
            m_pLarge.AttachSmallObjects(this);
        }
    }
}
