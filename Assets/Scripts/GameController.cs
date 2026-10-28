using UnityEngine;

/// <summary>Estados del flujo de juego: inicio -> juego -> fin.</summary>
public enum GameState
{
    Menu,
    Playing,
    Won,
    Lost
}

/// <summary>
/// Controla el flujo de la partida (menú, partida, victoria / derrota),
/// el puntaje y el récord. Los demás scripts le avisan de los eventos
/// importantes: enemigo eliminado, jugador muerto, jefe derrotado.
/// </summary>
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Referencias de escena")]
    public Player player;
    public FloorManager floor;

    [Header("Reglas")]
    [Tooltip("Cantidad de salas del piso (incluye inicio, tesoro y jefe).")]
    public int roomCount = 7;
    public int pointsBossBonus = 200;
    [Tooltip("Segundos máximos para recibir bono de tiempo al ganar.")]
    public int bonusTimeLimit = 300;

    public GameState State { get; private set; } = GameState.Menu;
    public int Score { get; private set; }
    public int Kills { get; private set; }
    public float RunTime { get; private set; }
    public int BestScore { get; private set; }

    const string BestKey = "Sotano.BestScore";

    void Awake()
    {
        Instance = this;
        // Juego cenital: no hay gravedad. El movimiento se hace con fuerzas / velocidades.
        Physics2D.gravity = Vector2.zero;
        Time.timeScale = 1f;
        BestScore = PlayerPrefs.GetInt(BestKey, 0);
    }

    void Start()
    {
        // En el menú se muestra la sala inicial de fondo.
        floor.Generate(roomCount);
        floor.EnterRoom(floor.StartRoom, -1);
        State = GameState.Menu;
    }

    void Update()
    {
        switch (State)
        {
            case GameState.Menu:
                if (Controls.SubmitPressed) StartRun();
                break;

            case GameState.Playing:
                RunTime += Time.deltaTime;
                break;

            default: // Won o Lost
                if (Controls.RestartPressed || Controls.SubmitPressed) StartRun();
                break;
        }
    }

    /// <summary>Empieza o reinicia una partida con un piso nuevo.</summary>
    public void StartRun()
    {
        Score = 0;
        Kills = 0;
        RunTime = 0f;

        player.ResetForRun();
        floor.Generate(roomCount);
        floor.EnterRoom(floor.StartRoom, -1);

        SoundManager.Instance.DuckMusic(false);
        State = GameState.Playing;
    }

    /// <summary>Llamado por un enemigo al morir.</summary>
    public void RegisterKill(int points)
    {
        Kills++;
        Score += points;
    }

    public void PlayerDied()
    {
        if (State == GameState.Playing) Finish(false);
    }

    public void BossDefeated()
    {
        if (State == GameState.Playing) Finish(true);
    }

    void Finish(bool won)
    {
        if (won)
        {
            Score += pointsBossBonus;
            Score += Mathf.Max(0, bonusTimeLimit - Mathf.RoundToInt(RunTime)) * 2;
        }

        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(BestKey, BestScore);
            PlayerPrefs.Save();
        }

        State = won ? GameState.Won : GameState.Lost;
        SoundManager.Instance.DuckMusic(true);
        if (won) SoundManager.Instance.PlayWin(); else SoundManager.Instance.PlayLose();
        CameraShake.Kick(won ? 0.4f : 0.3f);
    }
}
