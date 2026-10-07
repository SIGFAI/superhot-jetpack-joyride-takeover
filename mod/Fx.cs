// Shared look-and-feel helpers: cached materials, primitives with textures, embers, flames, explosions.
using System.Collections.Generic;
using Sigf.Kit;
using UnityEngine;

public static class Fx
{
    public static readonly Color Orange = new Color(1f, 0.5f, 0.08f);
    public static readonly Color Yellow = new Color(1f, 0.9f, 0.25f);
    public static readonly Color Cyan = new Color(0.35f, 0.9f, 1f);
    public static readonly Color Gold = new Color(1f, 0.78f, 0.12f);
    public static readonly Color Smoke = new Color(0.82f, 0.82f, 0.85f);

    static Material baseMat;
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    static Material Base()
    {
        if (baseMat == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = t.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(t);
        }
        return baseMat;
    }

    /// <summary>A cached material: tint, optional emission (glow) and optional PNG from mod/assets.</summary>
    public static Material Mat(Color c, float glow = 0f, string tex = null, float tile = 1f)
    {
        string key = c.ToString() + glow + tex + tile;
        if (mats.TryGetValue(key, out var m)) return m;
        m = new Material(Base());
        m.color = c;
        if (tex != null) { m.mainTexture = Mix.Texture(tex); m.mainTextureScale = new Vector2(tile, tile); }
        if (glow > 0f && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * glow);
            if (tex != null && m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", m.mainTexture);
        }
        mats[key] = m;
        return m;
    }

    public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 lpos, Vector3 lscale, Color c,
        string tex = null, float tile = 1f, float glow = 0f, bool collider = false, string name = "JJPart")
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        if (!collider) Object.Destroy(go.GetComponent<Collider>());
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = lpos;
        go.transform.localScale = lscale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = Mat(c, glow, tex, tile);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    public static void Kick(Vector3 p, float power)
    {
        if (G.Player != null && (G.Pos - p).sqrMagnitude < 30f * 30f) G.Shake(power);
    }

    /// <summary>Kills every enemy within r of p (the game's own shatter).</summary>
    public static void KillRadius(Vector3 p, float r)
    {
        foreach (var e in G.Enemies())
            if (e != null && !e.IsDead && (e.transform.position + Vector3.up * 1f - p).sqrMagnitude < r * r) G.Shatter(e);
    }

    public static void Explosion(Vector3 p, float radius = 4f, bool lethal = true)
    {
        var ball = Prim(PrimitiveType.Sphere, null, p, Vector3.one * 0.5f, Orange, null, 1, 3f, false, "JJBoom");
        ball.AddComponent<Grow>().Init(radius * 1.05f, 0.5f, true);
        var core = Prim(PrimitiveType.Sphere, null, p, Vector3.one * 0.3f, Yellow, null, 1, 5f, false, "JJBoomCore");
        core.AddComponent<Grow>().Init(radius * 0.6f, 0.3f, true);
        var l = Mix.Glow(p, Orange, radius * 4f, 9f);
        Object.Destroy(l.gameObject, 0.5f);
        for (int i = 0; i < 26; i++)
        {
            var v = (Random.onUnitSphere + Vector3.up * 0.4f) * Random.Range(4f, 11f);
            Ember.Spawn(p, v, Random.value < 0.5f ? Orange : Yellow, Random.Range(0.1f, 0.22f), Random.Range(0.6f, 1.4f), 3f, 0f, Vector3.down * 8f);
        }
        for (int i = 0; i < 12; i++)
            Ember.Spawn(p + Random.insideUnitSphere * radius * 0.3f, Random.onUnitSphere * 2.5f + Vector3.up, Smoke, Random.Range(0.3f, 0.55f), Random.Range(1.0f, 1.8f), 1f, 1.4f, Vector3.up * 0.8f, PrimitiveType.Sphere);
        Mix.Play(Mix.Sound("boom.wav"), p, 0.9f, Random.Range(0.9f, 1.1f));
        Kick(p, 1.4f);
        if (lethal) KillRadius(p, radius);
    }

    /// <summary>Flames pointing down from this transform; set the parent's rotation to aim them.</summary>
    public static Flame MakeFlame(Transform parent, Vector3 lpos, float size, bool light = false)
    {
        var holder = new GameObject("JJFlame");
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = lpos;
        var f = holder.AddComponent<Flame>();
        f.size = size;
        f.outer = Prim(PrimitiveType.Sphere, holder.transform, Vector3.zero, Vector3.one, Orange, null, 1, 1.4f).transform;
        f.inner = Prim(PrimitiveType.Sphere, holder.transform, Vector3.zero, Vector3.one, new Color(1f, 0.95f, 0.6f), null, 1, 2.2f).transform;
        if (light) f.light = Mix.Glow(holder.transform.position, Orange, 7f * size + 3f, 2.5f, holder.transform);
        return f;
    }

    /// <summary>A free spot in front of the player with clear line of sight (for coins, zappers, missiles).</summary>
    public static bool FreeSpot(float minDist, float maxDist, out Vector3 pos, float yawRange = 80f, float clearance = 1.2f)
    {
        pos = G.Pos;
        var cam = G.Cam;
        if (cam == null) return false;
        var from = cam.transform.position;
        var fwd = cam.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        for (int i = 0; i < 12; i++)
        {
            var dir = Quaternion.AngleAxis(Random.Range(-yawRange, yawRange), Vector3.up) * fwd.normalized;
            float d = maxDist;
            if (Physics.Raycast(from, dir, out var hit, maxDist, ~0, QueryTriggerInteraction.Ignore)) d = hit.distance - clearance;
            if (d < minDist) continue;
            pos = from + dir * Random.Range(minDist, d);
            return true;
        }
        return false;
    }

    public static float GroundY(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore)) return hit.point.y;
        return p.y - 1.6f;
    }
}

/// <summary>Cheap moving particle (a cube): embers, smoke puffs, sparks. Lives in game time, so freezes with SUPERHOT.</summary>
public class Ember : MonoBehaviour
{
    Vector3 vel, gravity;
    float life, t, size0, grow;

    public static Ember Spawn(Vector3 pos, Vector3 vel, Color c, float size, float life, float glow, float grow, Vector3 gravity, PrimitiveType shape = PrimitiveType.Cube)
    {
        var go = Fx.Prim(shape, null, pos, Vector3.one * size, c, null, 1, glow, false, "JJEmber");
        go.transform.rotation = Random.rotation;
        var e = go.AddComponent<Ember>();
        e.vel = vel; e.life = life; e.size0 = size; e.grow = grow; e.gravity = gravity;
        return e;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        if (t >= life) { Destroy(gameObject); return; }
        transform.position += vel * dt;
        vel += gravity * dt;
        float k = t / life;
        float s = size0 * (1f + grow * k) * (grow > 0f ? 1f - k * k * 0.6f : 1f - k * k);
        transform.localScale = Vector3.one * Mathf.Max(0.01f, s);
    }
}

/// <summary>Grows a shape then removes it (explosion fireballs).</summary>
public class Grow : MonoBehaviour
{
    float to, dur, t, from; bool fade;
    public void Init(float to, float dur, bool fade) { this.to = to; this.dur = dur; this.fade = fade; from = transform.localScale.x; }
    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / dur);
        float s = Mathf.Lerp(from, to, 1f - (1f - k) * (1f - k));
        if (fade && k > 0.6f) s *= 1f - (k - 0.6f) / 0.4f;
        transform.localScale = Vector3.one * Mathf.Max(0.01f, s);
        if (t >= dur) Destroy(gameObject);
    }
}

/// <summary>Flickering jet flame with trailing embers.</summary>
public class Flame : MonoBehaviour
{
    public float size = 1f;
    public Transform outer, inner;
    public Light light;
    public bool on = true;
    float seed, emit;
    void Awake() { seed = Random.value * 50f; }
    void Update()
    {
        if (outer == null) return;
        outer.gameObject.SetActive(on); inner.gameObject.SetActive(on);
        if (light != null) light.enabled = on;
        if (!on) return;
        float tt = Time.unscaledTime;
        float f = 1f + 0.3f * Mathf.Sin(tt * 47f + seed) + 0.2f * Mathf.Sin(tt * 23f + seed * 2f);
        outer.localScale = new Vector3(0.22f, 0.6f * f, 0.22f) * size;
        outer.localPosition = new Vector3(0f, -0.3f * f * size, 0f);
        inner.localScale = new Vector3(0.12f, 0.36f * f, 0.12f) * size;
        inner.localPosition = new Vector3(0f, -0.18f * f * size, 0f);
        if (light != null) light.intensity = 1.2f + 0.5f * Mathf.Sin(tt * 40f + seed);
        emit -= Time.deltaTime;
        if (emit <= 0f)
        {
            emit = 0.05f;
            var down = -transform.up;
            Ember.Spawn(transform.position + down * 0.4f * size, down * Random.Range(2f, 5f) + Random.insideUnitSphere * 1f,
                Random.value < 0.5f ? Fx.Orange : Fx.Yellow, 0.06f * size, Random.Range(0.4f, 0.9f), 3f, 0f, Vector3.down * 2f);
        }
    }
}

public static class Arena
{
    /// <summary>Finds the roomiest spot on the navmesh (long sight lines, high ceiling) and puts the player there
    /// looking down its longest line. Returns false when nothing better than the current spot exists.</summary>
    public static bool MoveToBest(int samples = 80)
    {
        var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
        if (tri.vertices == null || tri.vertices.Length < 3) return false;
        float eye = G.Cam != null ? G.Cam.transform.position.y - G.Pos.y : 0.7f;
        float groundY = Fx.GroundY(G.Pos), lift = G.Pos.y - groundY;
        float bestScore = Score(G.Pos, out var bestDir);
        Vector3 best = G.Pos;
        var rnd = new System.Random(7);
        for (int i = 0; i < samples; i++)
        {
            int t = rnd.Next(tri.indices.Length / 3) * 3;
            var c = (tri.vertices[tri.indices[t]] + tri.vertices[tri.indices[t + 1]] + tri.vertices[tri.indices[t + 2]]) / 3f;
            if (Mathf.Abs(c.y - groundY) > 1.5f) continue;
            var p = c + Vector3.up * lift;
            float s = Score(p, out var d);
            if (s > bestScore) { bestScore = s; best = p; bestDir = d; }
        }
        Mix.Log("ARENA score " + bestScore + " at " + best);
        if (best == G.Pos) { return false; }
        G.Warp(best, best + bestDir * 5f);
        return true;
    }

    /// <summary>A repeatable open stage near `center`: a free standing spot on the floor, as far from walls as possible.</summary>
    public static Vector3 OpenSpotNear(Vector3 center, float radius)
    {
        Vector3 best = center; float bestS = -1f;
        for (float dx = -radius; dx <= radius; dx += 1f)
            for (float dz = -radius; dz <= radius; dz += 1f)
            {
                var p = center + new Vector3(dx, 0f, dz);
                if (Physics.CheckCapsule(p + Vector3.up * 0.2f, p + Vector3.up * 1.2f, 0.45f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (!Physics.Raycast(p, Vector3.down, out var gh, 3f, ~0, QueryTriggerInteraction.Ignore) || gh.distance < 1.0f || gh.distance > 1.9f) continue;
                float minD = 99f, far = 0f;
                for (int a = 0; a < 8; a++)
                {
                    var d = Quaternion.Euler(0f, a * 45f, 0f) * Vector3.forward;
                    float len = 14f;
                    if (Physics.Raycast(p + Vector3.up * 0.2f, d, out var hit, 14f, ~0, QueryTriggerInteraction.Ignore)) len = hit.distance;
                    minD = Mathf.Min(minD, len); far = Mathf.Max(far, len);
                }
                float s = minD * 2f + far;
                if (s > bestS) { bestS = s; best = p; }
            }
        Mix.Log("OPENSPOT " + best + " score " + bestS);
        return best;
    }

    /// <summary>The player appears at p looking down the longest clear line (a repeatable stage for the demo).</summary>
    public static void WarpFacingOpen(Vector3 p)
    {
        Score(p, out var dir);
        G.Warp(p, p + dir * 5f);
    }

    static float Score(Vector3 p, out Vector3 bestDir)
    {
        float total = 0f, longest = 0f;
        bestDir = Vector3.forward;
        var o = p + Vector3.up * 1.2f;
        for (int a = 0; a < 12; a++)
        {
            var d = Quaternion.Euler(0f, a * 30f, 0f) * Vector3.forward;
            float len = 22f;
            if (Physics.Raycast(o, d, out var hit, 22f, ~0, QueryTriggerInteraction.Ignore)) len = hit.distance;
            total += len;
            if (len > longest) { longest = len; bestDir = d; }
        }
        float up = 12f;
        if (Physics.Raycast(o, Vector3.up, out var h2, 12f, ~0, QueryTriggerInteraction.Ignore)) up = h2.distance;
        return total / 12f + longest * 0.6f + up * 2.5f;
    }
}
