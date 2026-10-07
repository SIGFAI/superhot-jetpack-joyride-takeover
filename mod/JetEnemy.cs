// Every red crystal man gets a jetpack and hovers on twin flames. When one dies the pack tears loose,
// spins away, and blows up a moment later while the enemy bursts into coins.
using Sigf.Kit;
using UnityEngine;
using UnityEngine.AI;

public class JetEnemy : MonoBehaviour
{
    public Transform pack;
    NavMeshAgent agent;
    float phase, rise;
    public bool ejected;

    public static void Attach(PejAiController e)
    {
        if (e == null || e.GetComponent<JetEnemy>() != null) return;
        var je = e.gameObject.AddComponent<JetEnemy>();
        je.agent = e.GetComponent<NavMeshAgent>();
        je.phase = Random.value * 6f;
        je.pack = BuildPack(e.transform, new Vector3(0f, 1.32f, -0.27f), 1.25f);
    }

    /// <summary>The jetpack model: two orange tanks with red nose cones, a hazard-striped spine plate, nozzles and flames.</summary>
    public static Transform BuildPack(Transform parent, Vector3 lpos, float scale)
    {
        var root = new GameObject("JJJetpack").transform;
        root.SetParent(parent, false);
        root.localPosition = lpos;
        root.localScale = Vector3.one * scale;
        for (int s = -1; s <= 1; s += 2)
        {
            float x = s * 0.115f;
            Fx.Prim(PrimitiveType.Cylinder, root, new Vector3(x, 0f, 0f), new Vector3(0.2f, 0.22f, 0.2f), new Color(1f, 0.85f, 0.7f), "jetmetal.png", 1f, 0.15f);
            Fx.Prim(PrimitiveType.Sphere, root, new Vector3(x, 0.23f, 0f), new Vector3(0.2f, 0.16f, 0.2f), new Color(0.9f, 0.12f, 0.1f), null, 1f, 0.3f);
            Fx.Prim(PrimitiveType.Cylinder, root, new Vector3(x, -0.26f, 0f), new Vector3(0.13f, 0.05f, 0.13f), new Color(0.15f, 0.15f, 0.17f));
            Fx.Prim(PrimitiveType.Cylinder, root, new Vector3(x, -0.1f, 0f), new Vector3(0.215f, 0.035f, 0.215f), Color.white, "hazard.png", 1f, 0.2f);
            var holder = new GameObject("JJNozzle").transform;
            holder.SetParent(root, false);
            holder.localPosition = new Vector3(x, -0.3f, 0f);
            Fx.MakeFlame(holder, Vector3.zero, 0.9f, s < 0);
        }
        var src = root.gameObject.AddComponent<AudioSource>();
        src.clip = Mix.Sound("jet.wav"); src.loop = true; src.volume = 0.18f; src.pitch = Random.Range(0.85f, 1.1f);
        src.spatialBlend = 1f; src.minDistance = 4f; src.maxDistance = 28f;
        src.time = Random.value * 1.5f; src.Play();
        Fx.Prim(PrimitiveType.Cube, root, new Vector3(0f, 0f, 0.07f), new Vector3(0.32f, 0.4f, 0.05f), Color.white, "hazard.png", 1f, 0.2f);
        return root;
    }

    void Update()
    {
        if (ejected || agent == null) return;
        float dt = Time.deltaTime;
        rise = Mathf.MoveTowards(rise, 1.3f, dt * 1.5f);
        agent.baseOffset = rise + 0.08f * Mathf.Sin(Time.time * 3f + phase);
    }

    /// <summary>Called when the enemy dies: the pack becomes a flying, burning projectile and coins spill out.</summary>
    public void Eject()
    {
        if (ejected || pack == null) return;
        ejected = true;
        var p = pack.position;
        pack.SetParent(null, true);
        var rb = pack.gameObject.AddComponent<Rigidbody>();
        rb.mass = 2f;
        var col = pack.gameObject.AddComponent<SphereCollider>();
        col.radius = 0.2f;
        rb.velocity = new Vector3(Random.Range(-3f, 3f), Random.Range(5f, 8f), Random.Range(-3f, 3f));
        rb.angularVelocity = Random.insideUnitSphere * 14f;
        pack.gameObject.AddComponent<PackFuse>();
        Coin.Burst(p, 9);
        Mix.Burst(p, Fx.Orange, 12, 6f, 0.12f, 1.2f);
        Mix.Play(Mix.Sound("jet.wav"), p, 0.6f, 1.5f);
    }
}

/// <summary>A torn-off jetpack: burns for a moment (in game time), then explodes and takes nearby enemies with it.</summary>
public class PackFuse : MonoBehaviour
{
    float t;
    void Update()
    {
        t += Time.deltaTime;
        if (t > 1.1f)
        {
            Fx.Explosion(transform.position, 3.2f, true);
            Destroy(gameObject);
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(PejAiController), nameof(PejAiController.Kill))]
static class JetEnemyKill
{
    static void Prefix(PejAiController __instance)
    {
        if (__instance == null || __instance.IsDead) return;
        var je = __instance.GetComponent<JetEnemy>();
        if (je != null) je.Eject();
        else Coin.Burst(__instance.transform.position + Vector3.up * 1.2f, 5);
        Fx.Kick(__instance.transform.position, 0.5f);
    }
}
