// Jetpack Joyride missiles: a warning sign flashes on the edge of the screen, an alarm beeps, then the missile
// flies in on a flame trail. It explodes on anything it touches and kills every enemy in the blast.
using Sigf.Kit;
using UnityEngine;

public class Missile : MonoBehaviour
{
    public Transform Target;
    Vector3 dir;
    float speed = 13f, life, puff;
    bool done;

    public static void Warn(Vector3 from, float seconds)
    {
        Hud.Warnings.Add(new Hud.Warning { world = from, until = Time.unscaledTime + seconds });
        Mix.Play(Mix.Sound("warn.wav"), null, 0.8f);
    }

    /// <summary>Warns, then launches from `from` toward the player (or a point) after `delay` real seconds.</summary>
    public static void Incoming(Vector3 from, float delay = 1.6f, Transform follow = null)
    {
        Warn(from, delay);
        Mix.After(delay, () => Launch(from, null, follow), "MissileLaunch");
    }

    public static Missile Launch(Vector3 from, Vector3? target, Transform follow = null)
    {
        if (G.Cam == null) return null;
        var aim = target ?? (follow != null ? follow.position + Vector3.up * 1.2f : G.Cam.transform.position + Vector3.down * 0.3f);
        var go = new GameObject("JJMissile");
        go.transform.position = from;
        var m = go.AddComponent<Missile>();
        m.Target = follow;
        m.dir = (aim - from).normalized;
        go.transform.rotation = Quaternion.LookRotation(m.dir);
        var body = Fx.Prim(PrimitiveType.Cylinder, go.transform, Vector3.zero, new Vector3(0.34f, 0.5f, 0.34f), new Color(1f, 0.55f, 0.5f), "jetmetal.png", 1f, 0.35f, false, "JJMissileBody");
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Fx.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0f, 0f, 0.5f), new Vector3(0.34f, 0.34f, 0.5f), new Color(0.9f, 0.1f, 0.08f), null, 1f, 0.6f);
        var band = Fx.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0f, -0.1f), new Vector3(0.355f, 0.07f, 0.355f), Color.white, "hazard.png", 1f, 0.3f);
        band.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        for (int i = 0; i < 4; i++)
        {
            var fin = Fx.Prim(PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(0.04f, 0.3f, 0.3f), new Color(0.15f, 0.15f, 0.18f));
            fin.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            fin.transform.localPosition = fin.transform.localRotation * new Vector3(0f, 0.2f, -0.45f);
        }
        var holder = new GameObject("JJMissileFlame").transform;
        holder.SetParent(go.transform, false);
        holder.localPosition = new Vector3(0f, 0f, -0.55f);
        holder.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Fx.MakeFlame(holder, Vector3.zero, 1.8f, true);
        var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true;
        var col = go.AddComponent<SphereCollider>(); col.radius = 0.3f; col.isTrigger = true;
        Mix.Play(Mix.Sound("jet.wav"), from, 0.9f, 0.7f);
        return m;
    }

    void Update()
    {
        if (done) return;
        float dt = Time.deltaTime;
        life += dt;
        var cam = G.Cam;
        if (Target != null || cam != null)
        {
            var aimAt = Target != null ? Target.position + Vector3.up * 1.2f : cam.transform.position + Vector3.down * 0.3f;
            var want = (aimAt - transform.position).normalized;
            dir = Vector3.RotateTowards(dir, want, 0.5f * dt, 0f);
        }
        transform.rotation = Quaternion.LookRotation(dir);
        float d = speed * dt;
        if (Physics.SphereCast(transform.position, 0.3f, dir, out var hit, d + 0.2f, ~0, QueryTriggerInteraction.Ignore))
        {
            Boom(hit.point - dir * 0.3f);
            return;
        }
        transform.position += dir * d;
        if (cam != null && (transform.position - cam.transform.position).magnitude < 1.2f) { Boom(transform.position); return; }
        if (life > 12f) { Boom(transform.position); return; }
        puff -= dt;
        if (puff <= 0f)
        {
            puff = 0.035f;
            Ember.Spawn(transform.position - dir * 0.7f, -dir * 0.5f + Random.insideUnitSphere * 0.4f, Fx.Smoke, 0.14f, 1.5f, 1f, 3f, Vector3.up * 0.3f, PrimitiveType.Sphere);
        }
    }

    void Boom(Vector3 p)
    {
        done = true;
        Fx.Explosion(p, 4.5f, true);
        var cam = G.Cam;
        if (cam != null && !G.Player.IsDead() && (cam.transform.position - p).magnitude < 3f)
            G.Player.Kill(p);
        Destroy(gameObject);
    }
}
