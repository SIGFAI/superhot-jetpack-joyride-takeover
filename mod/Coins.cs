// Spinning gold coins in Jetpack Joyride formations; killed enemies burst into coins that fly to the player.
using Sigf.Kit;
using UnityEngine;

public class Coin : MonoBehaviour
{
    static float lastPick;
    static float combo;
    Vector3 vel;
    float age, magnetAfter, phase;
    bool magnet;

    public static Coin Spawn(Vector3 pos, Vector3 vel, float magnetAfter = -1f)
    {
        var root = new GameObject("JJCoin");
        root.transform.position = pos;
        var d = Fx.Prim(PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.55f, 0.03f, 0.55f), Color.white, "coin.png", 1f, 0.6f);
        d.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var c = root.AddComponent<Coin>();
        c.vel = vel; c.magnetAfter = magnetAfter; c.phase = Random.value * 360f;
        return c;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        transform.rotation = Quaternion.Euler(0f, phase + Time.time * 220f, 0f);
        var cam = G.Cam;
        if (cam == null) return;
        var eye = cam.transform.position;
        var body = G.Pos;
        if (vel.sqrMagnitude > 0.01f)
        {
            transform.position += vel * dt;
            vel *= Mathf.Pow(0.04f, dt);
        }
        float dEye = (transform.position - eye).magnitude;
        float dBody = (transform.position - (body + Vector3.up * 0.2f)).magnitude;
        if (magnetAfter >= 0f && age > magnetAfter && dEye < 9f) magnet = true;
        if (dEye < 2.6f) magnet = true;
        if (magnet)
        {
            var target = Vector3.Lerp(eye, body + Vector3.up * 0.5f, 0.4f);
            transform.position = Vector3.MoveTowards(transform.position, target, (6f + age * 2f) * Mathf.Max(dt, Time.unscaledDeltaTime * 0.15f));
        }
        if (Mathf.Min(dEye, dBody) < 1.15f) Pick();
    }

    void Pick()
    {
        float now = Time.unscaledTime;
        combo = now - lastPick < 1.2f ? Mathf.Min(combo + 1f, 14f) : 0f;
        lastPick = now;
        Hud.Coins++;
        Hud.Pulse = 1f;
        Mix.Play(Mix.Sound("coin.wav"), null, 0.55f, 1f + combo * 0.045f);
        for (int i = 0; i < 5; i++)
            Ember.Spawn(transform.position, Random.onUnitSphere * 3f, Fx.Gold, 0.07f, 0.5f, 3f, 0f, Vector3.down * 4f);
        Destroy(gameObject);
    }

    // ---------- formations (facing the viewer along `right`) ----------
    public static void Line(Vector3 origin, Vector3 right, int n, float step)
    {
        for (int i = 0; i < n; i++) Spawn(origin + right * ((i - (n - 1) / 2f) * step), Vector3.zero);
    }

    public static void Wave(Vector3 origin, Vector3 right, int n, float step, float amp)
    {
        for (int i = 0; i < n; i++)
            Spawn(origin + right * ((i - (n - 1) / 2f) * step) + Vector3.up * Mathf.Sin(i * 0.65f) * amp, Vector3.zero);
    }

    public static void Ring(Vector3 origin, Vector3 right, int n, float radius)
    {
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Spawn(origin + right * Mathf.Cos(a) * radius + Vector3.up * Mathf.Sin(a) * radius, Vector3.zero);
        }
    }

    public static void Heart(Vector3 origin, Vector3 right, int n, float scale)
    {
        for (int i = 0; i < n; i++)
        {
            float t = i * Mathf.PI * 2f / n;
            float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
            float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
            Spawn(origin + right * x * scale + Vector3.up * y * scale, Vector3.zero);
        }
    }

    public static void Burst(Vector3 pos, int n)
    {
        for (int i = 0; i < n; i++)
            Spawn(pos, (Random.onUnitSphere + Vector3.up * 0.7f) * Random.Range(3f, 7f), 0.5f);
    }

    /// <summary>A random formation 6..14 m in front of the player, on a free line of sight.</summary>
    public static void RandomFormation()
    {
        if (!Fx.FreeSpot(5f, 14f, out var p, 70f, 1.5f)) return;
        var cam = G.Cam.transform;
        var toward = cam.position - p; toward.y = 0f;
        var right = Vector3.Cross(Vector3.up, toward.normalized).normalized;
        float y = Mathf.Max(Fx.GroundY(p) + 1.1f, cam.position.y - 0.4f);
        var o = new Vector3(p.x, y, p.z);
        switch (Random.Range(0, 4))
        {
            case 0: Wave(o + Vector3.up * 0.6f, right, 14, 0.55f, 0.7f); break;
            case 1: Ring(o + Vector3.up * 0.8f, right, 12, 1.3f); break;
            case 2: Heart(o + Vector3.up * 1.1f, right, 22, 0.1f); break;
            default: Line(o, right, 9, 0.6f); break;
        }
    }
}
