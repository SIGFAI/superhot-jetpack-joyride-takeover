// The player's own jetpack: hold JUMP in the air (or after a hop) to climb on a machine-gun jet that rains bullets below.
using InputSystem;
using Sigf.Kit;
using UnityEngine;

public static class PlayerJet
{
    public static bool Force;          // the demo bot "holds" the jet
    public static bool Jetting;
    public const float MaxAlt = 5.5f;   // the jet hovers at this height above the floor
    public const float JetTime = 0.55f;
    static bool forced;
    static float groundY;
    static float prevForced = -1f;
    static float held, shotTimer, emitTimer;
    static AudioSource loop;
    static Vector3 lastPos;
    static bool haveLast;

    public static bool Pressed()
    {
        try { return Force || SHInput.movement.GetInput().jump; } catch { return Force; }
    }

    public static void Step(PlayerController pc)
    {
        float dt = Time.deltaTime;
        bool want = Pressed();
        var cc = pc.GetComponent<CharacterController>();
        if (want) held += Time.unscaledDeltaTime; else held = 0f;
        if (pc.IsGrounded) groundY = pc.transform.position.y;
        bool jet = want && (!pc.IsGrounded || held > 0.12f || Force);
        if (jet)
        {
            pc.lastJumpTime = Time.time;
            if (pc.IsGrounded) cc.Move(Vector3.up * 0.12f);
            float alt = pc.transform.position.y - groundY;
            float top = alt > MaxAlt ? 0f : 5f;
            pc.velocity.y = Mathf.MoveTowards(pc.velocity.y, top, 170f * dt);
            pc.allowSlomoJump = false;
            Fire(pc, dt);
        }
        if (jet) { if (!forced) prevForced = TimeControl.forcedTimeScale; TimeControl.forcedTimeScale = prevForced == -1f ? JetTime : Mathf.Max(prevForced, JetTime); forced = true; }
        else if (forced) { TimeControl.forcedTimeScale = prevForced; forced = false; }
        Jetting = jet;
        if (false) Mix.Log("JET y=" + pc.transform.position.y + " vy=" + pc.velocity.y + " grounded=" + pc.IsGrounded + " ts=" + Time.timeScale + " dt=" + dt);
        Hud.Heat = Mathf.MoveTowards(Hud.Heat, jet ? 1f : 0f, Time.unscaledDeltaTime * (jet ? 6f : 3f));
        Audio(jet);
        if (haveLast) { float moved = (pc.transform.position - lastPos).magnitude; if (moved < 1.5f) Hud.Meters += moved * 3f; }
        lastPos = pc.transform.position; haveLast = true;
        if (Hud.Meters > Hud.Best) Hud.Best = Hud.Meters;
    }

    static void Audio(bool on)
    {
        var cam = G.Cam;
        if (cam == null) return;
        if (loop == null)
        {
            var go = new GameObject("JJJetLoop");
            go.transform.SetParent(cam.transform, false);
            loop = go.AddComponent<AudioSource>();
            loop.clip = Mix.Sound("jet.wav");
            loop.loop = true; loop.volume = 0f; loop.spatialBlend = 0f;
            loop.Play();
        }
        loop.volume = Mathf.MoveTowards(loop.volume, on ? 0.55f : 0f, Time.unscaledDeltaTime * 5f);
        loop.pitch = 1f;
    }

    static void Fire(PlayerController pc, float dt)
    {
        var cam = G.Cam.transform;
        var origin = pc.transform.position + Vector3.down * 0.2f;
        emitTimer -= dt;
        if (emitTimer <= 0f)
        {
            emitTimer = 0.03f;
            var foot = cam.position + Vector3.down * 1.1f + Random.insideUnitSphere * 0.15f;
            Ember.Spawn(foot, Vector3.down * Random.Range(3f, 7f) + Random.insideUnitSphere * 1.2f, Random.value < 0.5f ? Fx.Orange : Fx.Yellow, 0.09f, Random.Range(0.4f, 0.8f), 3f, 0f, Vector3.down * 3f);
        }
        shotTimer -= dt;
        while (shotTimer <= 0f)
        {
            shotTimer += 0.09f;
            var fwd = cam.forward; fwd.y = 0f;
            var dir = (cam.forward + Vector3.down * 0.45f + Random.insideUnitSphere * 0.14f).normalized;
            var start = origin + Random.insideUnitSphere * 0.1f;
            Vector3 end = start + dir * 30f;
            bool hitSomething = false;
            if (Physics.Raycast(start, dir, out var hit, 30f, ~(1 << pc.gameObject.layer), QueryTriggerInteraction.Ignore))
            {
                end = hit.point; hitSomething = true;
                var e = hit.collider.GetComponentInParent<PejAiController>();
                if (e != null && !e.IsDead) G.Shatter(e);
            }
            var tr = Fx.Prim(PrimitiveType.Cube, null, (start + end) / 2f, new Vector3(0.04f, 0.04f, Vector3.Distance(start, end)), Fx.Yellow, null, 1f, 4f, false, "JJTracer");
            tr.transform.rotation = Quaternion.LookRotation(end - start);
            Object.Destroy(tr, 0.05f);
            if (hitSomething)
                for (int i = 0; i < 3; i++) Ember.Spawn(end, (hit.normal + Random.insideUnitSphere * 0.7f) * Random.Range(2f, 5f), Fx.Yellow, 0.05f, 0.4f, 3f, 0f, Vector3.down * 9f);
        }
    }
}

[HarmonyLib.HarmonyPatch(typeof(PlayerController), "UpdateMovement")]
static class PlayerJetPatch
{
    static void Postfix(PlayerController __instance)
    {
        try { PlayerJet.Step(__instance); }
        catch (System.Exception e) { Mix.Error("PlayerJet", e); }
    }
}
