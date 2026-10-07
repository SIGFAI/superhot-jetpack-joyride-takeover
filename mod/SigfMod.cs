// Jetpack Joyride Takeover: every crystal man straps on a jetpack, coins float in arcade formations, zappers and
// missiles roam the lab, and you get a machine-gun jetpack of your own (hold JUMP in the air).
using System.Collections;
using Sigf.Kit;
using UnityEngine;

public class SigfMod : MixMod
{
    float lastPos;

    public override void OnLoad() => G.StartLevel = "wareHouse3";

    public override void OnReady()
    {
        var hud = new GameObject("JJHud");
        hud.AddComponent<Hud>();
        Object.DontDestroyOnLoad(hud);
        Mix.Log("UP " + (Physics.Raycast(G.Pos, Vector3.up, out var uh, 50f) ? uh.distance : 99f) + " POS " + G.Pos + " scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        Mix.Say("JETPACK JOYRIDE", 3.5f, Fx.Gold, 0.18f, 84);
        Mix.Say("hold JUMP in the air to fly and spray bullets", 5f, Color.white, 0.28f, 38);
        Mix.Every(0.4f, AttachPacks, "packs");
        Mix.After(2f, Coin.RandomFormation, "firstcoins");
        Mix.Every(6f, () => { if (Object.FindObjectsOfType<Coin>().Length < 40) Coin.RandomFormation(); }, "coins");
        Mix.After(5f, () => { if (!Mix.DemoStarted) SpawnZapper(); }, "zapper1");
        Mix.Every(22f, () => { if (Mix.DemoStarted) return; if (Zapper.All.Count >= 3) Object.Destroy(Zapper.All[0].gameObject); SpawnZapper(); }, "zappers");
        Mix.Every(17f, () => { if (!Mix.DemoStarted && Fx.FreeSpot(14f, 24f, out var p, 90f, 1f)) Missile.Incoming(p + Vector3.up * 0.5f); }, "missiles");
        Mix.Every(9f, () => { if (!Mix.DemoStarted && G.Enemies().Count < 3) G.SpawnEnemy(G.Ahead(12f) + Random.insideUnitSphere * 3f); }, "enemies");
    }

    static void AttachPacks()
    {
        foreach (var e in G.Enemies()) JetEnemy.Attach(e);
    }

    static Zapper SpawnZapper()
    {
        if (!Fx.FreeSpot(6f, 14f, out var p, 60f, 2f)) return null;
        return Zapper.Create(new Vector3(p.x, Fx.GroundY(p) + 1.5f, p.z), 2.6f, Random.Range(0f, 180f), Random.Range(25f, 45f) * (Random.value < 0.5f ? 1f : -1f));
    }

    // a point `dist` m ahead of the camera (on the flat view direction), pulled back from walls
    static Vector3 Ahead(float dist, float side = 0f)
    {
        var cam = G.Cam.transform;
        var fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
        var right = Vector3.Cross(Vector3.up, fwd);
        var from = cam.position;
        float d = dist;
        if (Physics.Raycast(from, fwd, out var hit, dist + 2f, ~0, QueryTriggerInteraction.Ignore)) d = Mathf.Min(dist, hit.distance - 1.5f);
        return G.Pos + fwd * Mathf.Max(2.5f, d) + right * side;
    }

    public override IEnumerator Demo()
    {
        Hud.Coins = 0;
        Arena.WarpFacingOpen(Arena.OpenSpotNear(new Vector3(4.3f, 2.6f, 9.1f), 6f));
        yield return Mix.Wait(0.6f);
        var home = G.Pos;
        var homeLook = G.Pos + G.Cam.transform.forward * 5f;
        foreach (var zz in Zapper.All.ToArray()) Object.Destroy(zz.gameObject);
        G.ForceTime(0.6f);
        Mix.Say("JETPACK JOYRIDE", 3.5f, Fx.Gold, 0.18f, 90);
        G.Words("JETPACK;JOYRIDE");
        // ---- scene 1: jetpack enemies hover; the player lifts off, grabs coins and rakes them with the jet guns
        var c = G.Cam.transform;
        G.RemoveEnemies();
        var e1 = G.SpawnEnemy(Ahead(8f, -2.5f));
        var e2 = G.SpawnEnemy(Ahead(8f, 0f));
        var e3 = G.SpawnEnemy(Ahead(8f, 2.5f));
        var fwd = c.forward; fwd.y = 0f; fwd.Normalize();
        for (int i = 0; i < 8; i++) Coin.Spawn(G.Pos + fwd * 1.0f + Vector3.up * (0.8f + i * 0.55f), Vector3.zero);
        Coin.Heart(Ahead(7f) + Vector3.up * 3.6f, Vector3.Cross(Vector3.up, fwd), 24, 0.11f);
        yield return Mix.Run(G.LookAt(e2.transform.position + Vector3.up * 1.4f, 1.2f));
        yield return Mix.Wait(1.4f);
        PlayerJet.Force = true;
        for (int k = 0; k < 8; k++)
        {
            if (e2 != null && !e2.IsDead) Mix.Run(G.LookAt(e2.transform.position + Vector3.up * 0.8f, 0.4f));
            else Mix.Run(G.LookAt(Ahead(8f) + Vector3.down * 1f, 0.4f));
            yield return Mix.Wait(0.4f);
        }
        PlayerJet.Force = false;
        yield return Mix.Wait(2.5f);
        G.Warp(home, homeLook);
        yield return Mix.Wait(0.5f);
        G.RemoveEnemies();
        // ---- scene 2: a missile warning, the missile flies in, SUPERHOT time freezes beside an enemy, then the blast
        G.ForceTime(0.6f);
        var m1 = G.SpawnEnemy(Ahead(9f, -1.5f));
        var m2 = G.SpawnEnemy(Ahead(9f, 1.8f));
        var m3 = G.SpawnEnemy(Ahead(10f, 0.2f));
        yield return Mix.Run(G.LookAt(Ahead(9f) + Vector3.up * 1.5f, 0.8f));
        Vector3 from = Ahead(15f, 7f) + Vector3.up * 1.6f;
        Missile.Incoming(from, 1.6f, m1 != null ? m1.transform : null);
        float t = 0f;
        Missile ms = null;
        while (t < 7f)
        {
            t += Time.unscaledDeltaTime;
            if (ms == null) ms = Object.FindObjectOfType<Missile>();
            if (ms != null)
            {
                G.LookStep(ms.transform.position, 0.12f);
                if (m1 != null && !m1.IsDead && (ms.transform.position - m1.transform.position).magnitude < 4.2f)
                {
                    G.ForceTime(0.02f);
                    for (float f = 0f; f < 1.7f; f += Time.unscaledDeltaTime) { G.LookStep(ms.transform.position, 0.1f); yield return null; }
                    G.ForceTime(0.6f);
                    break;
                }
            }
            else if (t > 4f) break;
            yield return null;
        }
        yield return Mix.Wait(2.6f);
        G.Warp(home, homeLook);
        yield return Mix.Wait(0.5f);
        G.RemoveEnemies();
        // ---- scene 3: a spinning zapper fries an enemy
        var zp = Ahead(7f);
        var zc = new Vector3(zp.x, Fx.GroundY(zp) + 1.5f, zp.z);
        var z = Zapper.Create(zc, 2.8f, 20f, 38f);
        var victim = G.SpawnEnemy(new Vector3(zc.x, zc.y - 1.5f, zc.z) + (G.Cam.transform.right) * 0.7f);
        yield return Mix.Run(G.LookAt(zc, 0.8f));
        yield return Mix.Wait(4.5f);
        Mix.Say("kill enemies and they burst into coins", 4f, Color.white, 0.85f, 44);
        yield return Mix.Wait(3f);
    }
}
