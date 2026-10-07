// Jetpack Joyride style HUD: distance, coin counter, missile warning signs and the jetpack flame glow.
using System.Collections.Generic;
using Sigf.Kit;
using UnityEngine;

public class Hud : MonoBehaviour
{
    public static int Coins;
    public static float Meters, Best, Pulse, Heat;
    public static readonly List<Warning> Warnings = new List<Warning>();
    public class Warning { public Vector3 world; public float until; }

    GUIStyle big, small, tiny;
    Texture2D grad, white;

    void Style()
    {
        if (big != null) return;
        big = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
        small = new GUIStyle(big);
        tiny = new GUIStyle(big) { alignment = TextAnchor.MiddleCenter };
        grad = new Texture2D(1, 64, TextureFormat.RGBA32, false);
        for (int i = 0; i < 64; i++) grad.SetPixel(0, i, new Color(1f, 0.5f, 0.1f, Mathf.Pow(1f - i / 63f, 2.2f)));
        grad.Apply();
        white = Texture2D.whiteTexture;
    }

    void Text(Rect r, string s, GUIStyle st, int size, Color c, TextAnchor a = TextAnchor.UpperLeft)
    {
        st.fontSize = size; st.alignment = a;
        var prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.85f);
        float o = Mathf.Max(2f, size * 0.07f);
        foreach (var d in new[] { new Vector2(o, o), new Vector2(-o, o), new Vector2(o, -o), new Vector2(-o, -o) })
            GUI.Label(new Rect(r.x + d.x, r.y + d.y, r.width, r.height), s, st);
        GUI.color = c;
        GUI.Label(r, s, st);
        GUI.color = prev;
    }

    void OnGUI()
    {
        if (G.Player == null) return;
        Style();
        float k = Screen.height / 1080f;
        // jetpack heat at the bottom of the screen
        if (Heat > 0.01f)
        {
            float flick = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 40f);
            GUI.color = new Color(1f, 1f, 1f, Heat * flick * 0.85f);
            GUI.DrawTexture(new Rect(0, Screen.height * 0.55f, Screen.width, Screen.height * 0.45f), grad);
            GUI.color = Color.white;
        }
        // distance, top-left
        Text(new Rect(40 * k, 24 * k, 600 * k, 110 * k), string.Format("{0:0000}m", Meters), big, Mathf.RoundToInt(92 * k), Color.white);
        Text(new Rect(44 * k, 120 * k, 600 * k, 50 * k), "BEST " + Mathf.RoundToInt(Best) + "m", small, Mathf.RoundToInt(34 * k), new Color(1f, 1f, 1f, 0.7f));
        // coins, top-right
        float pulse = 1f + Pulse * 0.35f;
        float ic = 78f * k * pulse;
        var coin = Mix.Texture("coin.png");
        string cs = Coins.ToString();
        Text(new Rect(Screen.width - 520 * k, 28 * k, 400 * k, 110 * k), cs, big, Mathf.RoundToInt(86 * k * (1f + Pulse * 0.15f)), Fx.Gold, TextAnchor.UpperRight);
        GUI.DrawTexture(new Rect(Screen.width - 100 * k - ic * 0.2f, 34 * k - (ic - 78 * k) * 0.5f, ic, ic), coin);
        Pulse = Mathf.MoveTowards(Pulse, 0f, Time.unscaledDeltaTime * 4f);
        // missile warnings
        var cam = G.Cam;
        for (int i = Warnings.Count - 1; i >= 0; i--)
        {
            var w = Warnings[i];
            if (Time.unscaledTime > w.until) { Warnings.RemoveAt(i); continue; }
            Vector3 sp = cam.WorldToScreenPoint(w.world);
            if (sp.z < 0f) { sp.x = Screen.width - sp.x; sp.y = Screen.height * 0.5f; sp.x = sp.x < Screen.width / 2f ? 0f : Screen.width; }
            float m = 110f * k;
            float x = Mathf.Clamp(sp.x, m, Screen.width - m);
            float y = Mathf.Clamp(Screen.height - sp.y, m, Screen.height - m);
            float s = (150f + 25f * Mathf.Sin(Time.unscaledTime * 18f)) * k;
            bool vis = Mathf.Repeat(Time.unscaledTime * 6f, 1f) < 0.7f;
            if (vis)
            {
                GUI.DrawTexture(new Rect(x - s / 2f, y - s / 2f, s, s), Mix.Texture("warn.png"));
                Text(new Rect(x - 150 * k, y + s / 2f - 6 * k, 300 * k, 50 * k), "MISSILE!", tiny, Mathf.RoundToInt(34 * k), new Color(1f, 0.25f, 0.15f), TextAnchor.MiddleCenter);
            }
        }
    }
}
