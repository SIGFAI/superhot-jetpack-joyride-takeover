// A Jetpack Joyride zapper: two hazard-striped pylons joined by a crackling electric beam that slowly spins.
// The beam is solid, it fries any enemy that touches it (they shatter into coins) and kills the player.
using System.Collections.Generic;
using Sigf.Kit;
using UnityEngine;

public class Zapper : MonoBehaviour
{
    public static readonly List<Zapper> All = new List<Zapper>();
    Transform a, b, beamCol;
    LineRenderer lr, lr2;
    Light light;
    AudioSource hum;
    float len, angle, spin, jitterT, sparkT;
    Vector3 axis;   // the rotation plane normal
    CapsuleCollider cap;

    public static Zapper Create(Vector3 center, float len, float startAngle, float spinDegPerSec)
    {
        var go = new GameObject("JJZapper");
        go.transform.position = center;
        var z = go.AddComponent<Zapper>();
        z.len = len; z.angle = startAngle; z.spin = spinDegPerSec;
        var toward = G.Cam.transform.position - center; toward.y = 0f;
        z.axis = toward.sqrMagnitude < 0.01f ? Vector3.forward : toward.normalized;
        z.Build();
        All.Add(z);
        return z;
    }

    void Build()
    {
        a = MakeNode("JJNodeA");
        b = MakeNode("JJNodeB");
        lr = MakeLine(0.16f, Fx.Cyan, 4f);
        lr2 = MakeLine(0.05f, Color.white, 6f);
        var bc = new GameObject("JJBeamCollider");
        bc.transform.SetParent(transform, false);
        beamCol = bc.transform;
        cap = bc.AddComponent<CapsuleCollider>();
        cap.direction = 0; cap.radius = 0.14f; cap.height = len;
        var rb = bc.AddComponent<Rigidbody>(); rb.isKinematic = true;
        light = Mix.Glow(transform.position, Fx.Cyan, 9f, 2.5f, transform);
        hum = gameObject.AddComponent<AudioSource>();
        hum.clip = Mix.Sound("zap.wav"); hum.loop = true; hum.volume = 0.5f;
        hum.spatialBlend = 1f; hum.minDistance = 2f; hum.maxDistance = 30f;
        hum.Play();
        Place();
    }

    Transform MakeNode(string n)
    {
        var root = new GameObject(n).transform;
        root.SetParent(transform, false);
        Fx.Prim(PrimitiveType.Cylinder, root, new Vector3(0f, -0.2f, 0f), new Vector3(0.42f, 0.28f, 0.42f), Color.white, "hazard.png", 1f, 0.25f, true, "JJPylon");
        Fx.Prim(PrimitiveType.Sphere, root, new Vector3(0f, 0.12f, 0f), new Vector3(0.38f, 0.38f, 0.38f), Fx.Cyan, null, 1f, 4f, false, "JJOrb");
        return root;
    }

    LineRenderer MakeLine(float w, Color c, float glow)
    {
        var go = new GameObject("JJBeam");
        go.transform.SetParent(transform, false);
        var l = go.AddComponent<LineRenderer>();
        l.useWorldSpace = true;
        l.positionCount = 10;
        l.startWidth = l.endWidth = w;
        l.material = Fx.Mat(c, glow);
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return l;
    }

    void Place()
    {
        var plane = Vector3.Cross(Vector3.up, axis).normalized;      // horizontal axis in the rotation plane
        var u = Quaternion.AngleAxis(angle, axis) * plane;
        a.position = transform.position + u * len / 2f;
        b.position = transform.position - u * len / 2f;
        beamCol.position = transform.position;
        beamCol.rotation = Quaternion.FromToRotation(Vector3.right, u);
    }

    void OnDestroy() { All.Remove(this); }

    float SegDist(Vector3 p)
    {
        var pa = a.position + Vector3.up * 0.12f; var pb = b.position + Vector3.up * 0.12f;
        var ab = pb - pa;
        float t = Mathf.Clamp01(Vector3.Dot(p - pa, ab) / ab.sqrMagnitude);
        return (p - (pa + ab * t)).magnitude;
    }

    void Update()
    {
        angle += spin * Time.deltaTime;
        Place();
        var pa = a.position + Vector3.up * 0.12f; var pb = b.position + Vector3.up * 0.12f;
        // beam crackle in real time so it always looks alive
        jitterT -= Time.unscaledDeltaTime;
        if (jitterT <= 0f)
        {
            jitterT = 0.04f;
            var perp = Vector3.Cross(pb - pa, Vector3.up).normalized;
            for (int i = 0; i < lr.positionCount; i++)
            {
                float t = i / (float)(lr.positionCount - 1);
                float j = (i == 0 || i == lr.positionCount - 1) ? 0f : 0.22f;
                var p = Vector3.Lerp(pa, pb, t) + (perp + Vector3.up).normalized * Random.Range(-j, j) + Vector3.Cross(perp, Vector3.up) * Random.Range(-j, j) * 0.5f;
                lr.SetPosition(i, p);
                lr2.SetPosition(i, Vector3.Lerp(p, Vector3.Lerp(pa, pb, t), 0.4f));
            }
            light.intensity = Random.Range(1.5f, 4f);
        }
        sparkT -= Time.deltaTime;
        if (sparkT <= 0f)
        {
            sparkT = 0.08f;
            var sp = Vector3.Lerp(pa, pb, Random.value);
            Ember.Spawn(sp, Random.onUnitSphere * 2.5f, Fx.Cyan, 0.05f, 0.4f, 4f, 0f, Vector3.down * 3f);
        }
        foreach (var e in G.EnemiesCached())
        {
            if (e == null || e.IsDead) continue;
            var ep = e.transform.position;
            if (SegDist(ep + Vector3.up * 0.4f) < 0.7f || SegDist(ep + Vector3.up * 1.0f) < 0.7f || SegDist(ep + Vector3.up * 1.6f) < 0.7f)
            {
                Burn(ep + Vector3.up * 1f);
                G.Shatter(e);
            }
        }
        var cam = G.Cam;
        if (cam != null && !G.Player.IsDead() && (SegDist(cam.transform.position) < 0.75f || SegDist(G.Pos) < 0.75f))
        {
            Burn(cam.transform.position);
            G.Player.Kill(cam.transform.position + cam.transform.forward);
        }
    }

    static void Burn(Vector3 p)
    {
        Mix.Burst(p, Fx.Cyan, 18, 8f, 0.1f, 1f);
        var l = Mix.Glow(p, Fx.Cyan, 8f, 6f);
        Destroy(l.gameObject, 0.25f);
        Mix.Play(Mix.Sound("zap.wav"), p, 0.9f, 1.6f);
        Fx.Kick(p, 1f);
    }
}
