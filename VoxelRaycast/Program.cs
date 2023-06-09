using SimulationFramework;
using SimulationFramework.Drawing;
using System.Numerics;

new Sim().Run();

class Sim : Simulation
{
    const int size = 16;

    Vector2 ro;
    Vector2 rd;
    int[] voxels = new int[size * size];

    public override void OnInitialize(AppConfig config)
    {
        voxels[16 * 8 + 8] = 255;
    }

    public override void OnRender(ICanvas canvas)
    {
        canvas.Translate(canvas.Width / 2f, canvas.Height / 2f);
        canvas.Scale(canvas.Height / 20f);
        canvas.Translate(-10, -10);

        Matrix3x2.Invert(canvas.State.Transform, out var inv);
        Vector2 mp = Vector2.Transform(Mouse.Position, inv);

        if (Mouse.IsButtonDown(MouseButton.Left))
            rd = (mp - ro).Normalized();

        canvas.Clear(Color.Black);

        canvas.Stroke(Color.White);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                canvas.Stroke(new Color(voxels[i * size + j], 255, 255));

                canvas.DrawRect(i, j, 1, 1, Alignment.TopLeft);
            }
        }

        if (Raycast2(ro,rd,canvas,out var vox))
        {
            canvas.Fill(Color.Red);
            canvas.DrawRect(vox, Vector2.One);
        }

        Vector2 d = default;
        if (Keyboard.IsKeyDown(Key.W))
            d += new Vector2(0, -1);
        if (Keyboard.IsKeyDown(Key.A))
            d += new Vector2(-1, 0);
        if (Keyboard.IsKeyDown(Key.S))
            d += new Vector2(0, 1);
        if (Keyboard.IsKeyDown(Key.D))
            d += new Vector2(1, 0);

        ro += 10 * d * Time.DeltaTime;
    }

    public bool Raycast2(Vector2 origin, Vector2 direction, ICanvas canvas, out Vector2 voxel)
    {
        direction = direction.Normalized();

        voxel = new((int)MathF.Floor(origin.X), (int)MathF.Floor(origin.Y));
        Vector2 step = new(MathF.Sign(direction.X), MathF.Sign(direction.Y));

        if (step.X is 0 && step.Y is 0)
            return false;

        float tNear, tFar;
        Box box;
        box.min = new Vector2(0, 0);
        box.max = new Vector2(16, 16);
        box.PartialRaycast(origin, direction, out tNear, out tFar);

        Vector2 start = origin + direction * MathF.Max(0, tNear);
        Vector2 end = origin + direction * tFar;

        canvas.DrawLine(start, end);

        Vector2 d = end - start;

        Vector2 tDelta = (step) / d;

        // tDelta.X = MathF.Abs(tDelta.X);
        // tDelta.Y = MathF.Abs(tDelta.Y);
        // tDelta.Z = MathF.Abs(tDelta.Z);

        Vector2 tMax = tDelta * new Vector2(Frac(start.X, step.X), Frac(start.Y, step.Y));

        float Frac(float f, float s)
        {
            if (s > 0)
                return 1 - f + MathF.Floor(f);
            else
                return f - MathF.Floor(f);
        }
        float t = 0;
        while (true)
        {
            if (step.X is not 0 && tMax.X < tMax.Y)
            {
                voxel.X += step.X;
                tMax.X += tDelta.X;
                t += tDelta.X;
            }
            else
            {
                if (step.Y is 0)
                    return false;

                voxel.Y += step.Y;
                tMax.Y += tDelta.Y;
                t += tDelta.Y;
            }

            if (voxel.X < 0 || voxel.X >= 16 || voxel.Y < 0 || voxel.Y >= 16)
            {
                canvas.DrawCircle(start + direction * MathF.Max((tMax.X - tDelta.X), tMax.Y - tDelta.Y) * d.Length(), .1f);
                return false;
            }

            if (voxels[(int)(voxel.Y * 16 + voxel.X)] is not 0)
            {
                canvas.DrawCircle(start + direction * MathF.Max((tMax.X - tDelta.X), tMax.Y - tDelta.Y) * d.Length(), .1f);
                return true;
            }

            canvas.Stroke(Color.Yellow);
            canvas.DrawRect(voxel, Vector2.One);
        }

        // return voxel.X < 0 || voxel.X >= 16 || voxel.Y < 0 || voxel.Y >= 16 || voxel.Z < 0 || voxel.Z >= 16;
    }
}

struct Box
{
    public Vector2 min;
    public Vector2 max;


    public bool PartialRaycast(Vector2 origin, Vector2 direction, out float tNear, out float tFar)
    {
        float t1 = (min.X - origin.X) * 1f / direction.X;
        float t2 = (max.X - origin.X) * 1f / direction.X;
        float t3 = (min.Y - origin.Y) * 1f / direction.Y;
        float t4 = (max.Y - origin.Y) * 1f / direction.Y;

        tNear = MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4));
        tFar = MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4));

        if (tNear <= tFar && tFar > 0)
        {
            return true;
        }

        return false;
    }
}