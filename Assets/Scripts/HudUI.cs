using UnityEngine;

/// <summary>
/// Interfaz del juego dibujada con IMGUI (OnGUI): menú de inicio, HUD
/// (corazones, estadísticas, minimapa, barra del jefe) y pantalla de fin.
/// No necesita ningún paquete adicional de Unity.
/// </summary>
public class HudUI : MonoBehaviour
{
    [Header("Iconos de corazón")]
    public Texture2D heartFull;
    public Texture2D heartHalf;
    public Texture2D heartEmpty;

    GUIStyle titleStyle, bodyStyle, smallStyle, buttonStyle;

    void EnsureStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 76, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperLeft };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 36, fontStyle = FontStyle.Bold };
    }

    void OnGUI()
    {
        GameController gc = GameController.Instance;
        if (gc == null) return;
        EnsureStyles();

        // Diseñamos para 720 px de alto y escalamos a cualquier resolución.
        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float w = Screen.width / s;
        const float h = 720f;

        if (gc.State == GameState.Menu)
        {
            DrawMenu(gc, w, h);
            return;
        }

        DrawHud(gc, w, h);
        if (gc.State != GameState.Playing) DrawEnd(gc, w, h);
    }

    // ----------------------------------------------------------------- Pantallas

    void DrawMenu(GameController gc, float w, float h)
    {
        Fill(new Rect(0, 0, w, h), new Color(0.03f, 0.01f, 0.04f, 0.82f));

        Text(new Rect(0, 110, w, 100), "LÁGRIMAS DEL SÓTANO", titleStyle, new Color(0.95f, 0.8f, 0.7f));
        Text(new Rect(0, 230, w, 200),
             "Recorre las salas, derrota a los enemigos y vence al jefe.\n" +
             "Las puertas se abren al limpiar cada sala.\n\n" +
             "WASD: moverse     Flechas: disparar\nM: silenciar sonido",
             bodyStyle, Color.white);

        if (GUI.Button(new Rect(w / 2f - 170f, 470f, 340f, 90f), "JUGAR", buttonStyle)) gc.StartRun();
        Text(new Rect(0, 575, w, 40), "o pulsa Enter", smallStyle, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperCenter);
        Text(new Rect(0, 640, w, 40), "Récord: " + gc.BestScore, smallStyle, new Color(1f, 0.9f, 0.5f), TextAnchor.UpperCenter);
    }

    void DrawHud(GameController gc, float w, float h)
    {
        Player p = Player.Instance;
        FloorManager f = FloorManager.Instance;

        // Corazones: cada uno vale 2 puntos de vida.
        int hearts = p.maxHealth / 2;
        for (int i = 0; i < hearts; i++)
        {
            int remaining = p.Health - i * 2;
            Texture2D icon = remaining >= 2 ? heartFull : (remaining == 1 ? heartHalf : heartEmpty);
            if (icon != null) GUI.DrawTexture(new Rect(20 + i * 46, 16, 44, 44), icon, ScaleMode.StretchToFill, true);
        }

        Text(new Rect(22, 66, 400, 100),
             "Daño " + p.Damage.ToString("0.0") + "   Cadencia " + p.FireRate.ToString("0.0") +
             "   Velocidad " + p.MoveSpeed.ToString("0.0") + "\nPuntos " + gc.Score +
             "   Salas limpias " + f.RoomsCleared,
             smallStyle, Color.white);

        DrawMinimap(f, w);

        // Barra de vida del jefe.
        if (f.CurrentBoss != null && gc.State == GameState.Playing)
        {
            float ratio = Mathf.Clamp01(f.CurrentBoss.Health / f.CurrentBoss.MaxHealth);
            Rect bar = new Rect(w / 2f - 250f, h - 58f, 500f, 20f);
            Text(new Rect(bar.x, bar.y - 30f, bar.width, 28f), "JEFE", smallStyle, new Color(1f, 0.6f, 0.6f), TextAnchor.UpperCenter);
            Fill(new Rect(bar.x - 3, bar.y - 3, bar.width + 6, bar.height + 6), new Color(0f, 0f, 0f, 0.8f));
            Fill(new Rect(bar.x, bar.y, bar.width * ratio, bar.height), new Color(0.85f, 0.15f, 0.2f));
        }

        Text(new Rect(0, h - 28, w, 24), "WASD: mover    Flechas: disparar    M: silencio",
             smallStyle, new Color(1f, 1f, 1f, 0.55f), TextAnchor.UpperCenter);
    }

    void DrawMinimap(FloorManager f, float w)
    {
        const float cell = 22f, step = 27f;
        float cx = w - 100f, cy = 90f;

        foreach (var pair in f.Rooms)
        {
            Room r = pair.Value;
            bool known = r.visited;
            if (!known)
            {
                // Se muestran las salas vecinas de una sala ya visitada (aún sin explorar).
                foreach (var other in f.Rooms.Values)
                {
                    if (!other.visited) continue;
                    Vector2Int d = other.pos - r.pos;
                    if (Mathf.Abs(d.x) + Mathf.Abs(d.y) == 1) { known = true; break; }
                }
            }
            if (!known) continue;

            Color c = new Color(0.55f, 0.55f, 0.6f, 0.55f);
            if (r.visited) c = new Color(0.85f, 0.85f, 0.9f, 0.95f);
            if (r.visited && r.type == RoomType.Boss) c = new Color(0.9f, 0.25f, 0.25f, 0.95f);
            if (r.visited && r.type == RoomType.Item) c = new Color(1f, 0.85f, 0.3f, 0.95f);
            if (r == f.Current) c = new Color(0.4f, 1f, 0.5f, 1f);

            Fill(new Rect(cx + r.pos.x * step - cell / 2f, cy - r.pos.y * step - cell / 2f, cell, cell), c);
        }
    }

    void DrawEnd(GameController gc, float w, float h)
    {
        bool won = gc.State == GameState.Won;
        Fill(new Rect(0, 0, w, h), new Color(0.03f, 0.01f, 0.04f, 0.8f));

        Text(new Rect(0, 130, w, 100), won ? "¡VICTORIA!" : "HAS MUERTO", titleStyle,
             won ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.4f, 0.4f));
        Text(new Rect(0, 250, w, 220),
             "Puntaje: " + gc.Score + "\n" +
             "Enemigos derrotados: " + gc.Kills + "\n" +
             "Salas limpias: " + FloorManager.Instance.RoomsCleared + "\n" +
             "Tiempo: " + Mathf.RoundToInt(gc.RunTime) + " s\n" +
             "Récord: " + gc.BestScore,
             bodyStyle, Color.white);

        if (GUI.Button(new Rect(w / 2f - 210f, 500f, 420f, 90f), "JUGAR DE NUEVO", buttonStyle)) gc.StartRun();
        Text(new Rect(0, 605, w, 40), "o pulsa R / Enter", smallStyle, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperCenter);
    }

    // ------------------------------------------------------------------- Ayudas

    static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    // Texto con sombra para que se lea sobre cualquier fondo.
    static void Text(Rect r, string text, GUIStyle style, Color color, TextAnchor? align = null)
    {
        TextAnchor oldAlign = style.alignment;
        Color oldColor = style.normal.textColor;
        if (align.HasValue) style.alignment = align.Value;

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = color;
        GUI.Label(r, text, style);

        style.normal.textColor = oldColor;
        style.alignment = oldAlign;
    }
}
