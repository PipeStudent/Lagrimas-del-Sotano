using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Start,
    Normal,
    Item,   // cuarto del tesoro: una mejora gratis
    Boss
}

/// <summary>Datos lógicos de una sala del piso (el contenido se crea al entrar).</summary>
public class Room
{
    public Vector2Int pos;
    public RoomType type;
    public bool cleared;
    public bool visited;
    public bool itemTaken;
    public int seed;
    public int distance = -1; // pasos desde la sala inicial
}

/// <summary>
/// Genera el piso (salas conectadas en una cuadrícula) y gestiona la sala
/// actual: crea rocas y enemigos al entrar, bloquea las puertas hasta
/// limpiar la sala y mueve al jugador entre salas.
/// Todas las salas comparten el mismo "cascarón" (fondo, paredes y puertas),
/// así que la cámara es fija y solo cambia el contenido.
/// </summary>
public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject rockPrefab;
    public GameObject chaserPrefab;
    public GameObject shooterPrefab;
    public GameObject bossPrefab;
    public GameObject pickupPrefab;

    [Header("Sprites de objetos (orden de PickupKind)")]
    public Sprite[] pickupSprites;

    [Header("Puertas (0 arriba, 1 derecha, 2 abajo, 3 izquierda)")]
    public Door[] doors;

    [Header("Probabilidades de botín al limpiar una sala")]
    [Range(0f, 1f)] public float heartDropChance = 0.30f;
    [Range(0f, 1f)] public float upgradeDropChance = 0.12f;

    // Direcciones de viaje y dónde aparece el jugador al llegar (junto a la puerta opuesta).
    static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };
    static readonly Vector2[] Arrival =
    {
        new Vector2(0f, -3.4f), new Vector2(-6.4f, 0f), new Vector2(0f, 3.4f), new Vector2(6.4f, 0f)
    };

    public Dictionary<Vector2Int, Room> Rooms { get; private set; } = new Dictionary<Vector2Int, Room>();
    public Room StartRoom { get; private set; }
    public Room Current { get; private set; }
    public Enemy CurrentBoss { get; private set; }
    public int RoomsCleared { get; private set; }

    Transform content;
    int enemiesAlive;
    float nextMoveAllowed;
    readonly List<Vector2> rockPositions = new List<Vector2>();

    void Awake()
    {
        Instance = this;
        content = new GameObject("RoomContent").transform;
    }

    // ------------------------------------------------------------ Generación

    /// <summary>Crea un piso nuevo con "count" salas (caminata aleatoria en cuadrícula).</summary>
    public void Generate(int count)
    {
        Rooms.Clear();
        RoomsCleared = 0;
        var rng = new System.Random();

        StartRoom = new Room { pos = Vector2Int.zero, type = RoomType.Start, cleared = true, seed = rng.Next() };
        Rooms[StartRoom.pos] = StartRoom;

        var list = new List<Room> { StartRoom };
        int guard = 0;
        while (list.Count < Mathf.Max(4, count) && guard++ < 1000)
        {
            Room from = list[rng.Next(list.Count)];
            Vector2Int p = from.pos + Dirs[rng.Next(4)];
            if (Rooms.ContainsKey(p)) continue;

            var room = new Room { pos = p, type = RoomType.Normal, seed = rng.Next() };
            Rooms[p] = room;
            list.Add(room);
        }

        // Distancias desde el inicio (recorrido en anchura).
        var queue = new Queue<Room>();
        StartRoom.distance = 0;
        queue.Enqueue(StartRoom);
        while (queue.Count > 0)
        {
            Room r = queue.Dequeue();
            foreach (Vector2Int d in Dirs)
            {
                Room n;
                if (Rooms.TryGetValue(r.pos + d, out n) && n.distance < 0)
                {
                    n.distance = r.distance + 1;
                    queue.Enqueue(n);
                }
            }
        }

        // Jefe: la sala más lejana del inicio.
        Room boss = null;
        foreach (Room r in list)
            if (r != StartRoom && (boss == null || r.distance > boss.distance)) boss = r;
        boss.type = RoomType.Boss;

        // Tesoro: preferimos un callejón sin salida (una sola puerta).
        var candidates = new List<Room>();
        foreach (Room r in list)
        {
            if (r == StartRoom || r == boss) continue;
            if (NeighborCount(r) == 1) candidates.Add(r);
        }
        if (candidates.Count == 0)
            foreach (Room r in list)
                if (r != StartRoom && r != boss) candidates.Add(r);

        Room treasure = candidates[rng.Next(candidates.Count)];
        treasure.type = RoomType.Item;
        treasure.cleared = true;
    }

    int NeighborCount(Room r)
    {
        int n = 0;
        foreach (Vector2Int d in Dirs)
            if (Rooms.ContainsKey(r.pos + d)) n++;
        return n;
    }

    // --------------------------------------------------------- Entrar a salas

    /// <summary>Muestra una sala. "travelDir" es la dirección por la que se llegó (-1 = inicio).</summary>
    public void EnterRoom(Room room, int travelDir)
    {
        Current = room;
        room.visited = true;
        CurrentBoss = null;
        enemiesAlive = 0;
        rockPositions.Clear();

        ClearContent();

        // Puertas: solo existen las que tienen sala vecina; abiertas si la sala está limpia.
        for (int d = 0; d < 4; d++)
            doors[d].SetState(Rooms.ContainsKey(room.pos + Dirs[d]), room.cleared);

        Vector2 entry = travelDir >= 0 ? Arrival[travelDir] : new Vector2(0f, -1f);
        Player.Instance.Teleport(travelDir >= 0 ? entry : Vector2.zero);

        var rng = new System.Random(room.seed); // misma disposición cada vez que se vuelve a entrar
        if (room.type == RoomType.Normal) SpawnRocks(rng);

        if (!room.cleared)
        {
            if (room.type == RoomType.Boss) SpawnBoss();
            else SpawnEnemies(room, rng, entry);
        }

        if (room.type == RoomType.Item && !room.itemTaken)
        {
            var kinds = new[] { PickupKind.DamageUp, PickupKind.RateUp, PickupKind.SpeedUp };
            SpawnPickup(kinds[rng.Next(kinds.Length)], new Vector2(0f, 0.5f), true);
        }
    }

    /// <summary>Lo llaman las puertas abiertas cuando el jugador las toca.</summary>
    public void TryMove(int direction)
    {
        if (GameController.Instance.State != GameState.Playing) return;
        if (Time.time < nextMoveAllowed) return;

        Room target;
        if (!Rooms.TryGetValue(Current.pos + Dirs[direction], out target)) return;

        nextMoveAllowed = Time.time + 0.4f;
        SoundManager.Instance.PlayDoor();
        EnterRoom(target, direction);
    }

    void ClearContent()
    {
        foreach (Transform child in content) Destroy(child.gameObject);
        foreach (Tear t in FindObjectsByType<Tear>(FindObjectsSortMode.None)) Destroy(t.gameObject);
    }

    // ---------------------------------------------------------------- Contenido

    void SpawnRocks(System.Random rng)
    {
        int count = rng.Next(3, 8);
        var used = new HashSet<Vector2Int>();
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(-6, 7);
            int y = rng.Next(-3, 4);

            // Deja libres los pasillos frente a las puertas y los puntos de llegada.
            if (Mathf.Abs(x) <= 1 && Mathf.Abs(y) >= 2) continue;
            if (Mathf.Abs(y) <= 1 && Mathf.Abs(x) >= 5) continue;
            if (!used.Add(new Vector2Int(x, y))) continue;

            Instantiate(rockPrefab, new Vector3(x, y, 0f), Quaternion.identity, content);
            rockPositions.Add(new Vector2(x, y));
        }
    }

    void SpawnEnemies(Room room, System.Random rng, Vector2 playerEntry)
    {
        int count = Mathf.Min(5, rng.Next(2, 5) + (room.distance >= 3 ? 1 : 0));
        for (int i = 0; i < count; i++)
        {
            Vector2 p = FindFreeSpot(rng, playerEntry);
            GameObject prefab = rng.NextDouble() < 0.6 ? chaserPrefab : shooterPrefab;
            Instantiate(prefab, p, Quaternion.identity, content);
            enemiesAlive++;
        }
    }

    void SpawnBoss()
    {
        GameObject go = Instantiate(bossPrefab, new Vector3(0f, 1.5f, 0f), Quaternion.identity, content);
        CurrentBoss = go.GetComponent<Enemy>();
        enemiesAlive++;
    }

    // Busca un punto lejos del jugador y de las rocas (hasta 40 intentos).
    Vector2 FindFreeSpot(System.Random rng, Vector2 playerEntry)
    {
        Vector2 p = Vector2.zero;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            p = new Vector2((float)(rng.NextDouble() * 12.0 - 6.0), (float)(rng.NextDouble() * 6.0 - 3.0));
            if (Vector2.Distance(p, playerEntry) < 3.5f) continue;

            bool blocked = false;
            foreach (Vector2 rock in rockPositions)
                if (Vector2.Distance(p, rock) < 1.2f) { blocked = true; break; }
            if (!blocked) return p;
        }
        return p;
    }

    void SpawnPickup(PickupKind kind, Vector2 position, bool roomItem)
    {
        GameObject go = Instantiate(pickupPrefab, position, Quaternion.identity, content);
        go.GetComponent<Pickup>().Setup(kind, pickupSprites[(int)kind], roomItem);
    }

    public void MarkItemTaken()
    {
        if (Current != null) Current.itemTaken = true;
    }

    // ------------------------------------------------------------------ Eventos

    /// <summary>Lo llama cada enemigo al morir. Si era el último, se limpia la sala.</summary>
    public void OnEnemyKilled(Enemy enemy)
    {
        if (enemy == CurrentBoss) CurrentBoss = null;
        enemiesAlive--;
        if (enemiesAlive > 0) return;

        Current.cleared = true;
        RoomsCleared++;

        if (Current.type == RoomType.Boss)
        {
            GameController.Instance.BossDefeated();
            return;
        }

        for (int d = 0; d < 4; d++)
            doors[d].SetState(Rooms.ContainsKey(Current.pos + Dirs[d]), true);
        SoundManager.Instance.PlayDoor();

        // Botín aleatorio al limpiar la sala.
        float roll = Random.value;
        Vector2 dropPos = new Vector2(0f, 0f);
        if (roll < upgradeDropChance)
        {
            var kinds = new[] { PickupKind.DamageUp, PickupKind.RateUp, PickupKind.SpeedUp };
            SpawnPickup(kinds[Random.Range(0, kinds.Length)], dropPos, false);
        }
        else if (roll < upgradeDropChance + heartDropChance)
        {
            SpawnPickup(PickupKind.Heart, dropPos, false);
        }
    }
}
