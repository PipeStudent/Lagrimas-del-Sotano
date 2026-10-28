using UnityEngine;

public enum EnemyKind
{
    Chaser,   // persigue al jugador y daña al tocarlo
    Shooter,  // deambula y dispara al jugador
    Boss      // persigue lento y dispara anillos de lágrimas
}

/// <summary>
/// Enemigo con vida, tres comportamientos y daño por contacto. Al aparecer
/// espera un instante (parpadea) antes de actuar, para que el jugador pueda reaccionar.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public EnemyKind kind;

    [Header("Estadísticas")]
    public float maxHealth = 6f;
    public float speed = 2.8f;
    public float acceleration = 25f;
    public int contactDamage = 1;
    public int scoreValue = 10;
    [Tooltip("Segundos que tarda en actuar tras aparecer.")]
    public float spawnDelay = 0.7f;

    [Header("Disparo (Shooter y Boss)")]
    public GameObject tearPrefab;
    public float fireInterval = 2f;
    public float tearSpeed = 4.5f;

    public float Health { get; private set; }
    public float MaxHealth { get { return maxHealth; } }

    Rigidbody2D rb;
    SpriteRenderer sprite;
    Color baseColor;
    float activeAt, stunUntil, flashUntil, nextFire, nextWander, ringOffset;
    Vector2 wanderDir;
    bool dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        baseColor = sprite.color;
        Health = maxHealth;
        activeAt = Time.time + spawnDelay;
        nextFire = activeAt + Random.Range(0.4f, fireInterval);
    }

    bool Playing
    {
        get { return GameController.Instance != null && GameController.Instance.State == GameState.Playing; }
    }

    void Update()
    {
        // Aspecto: semitransparente al aparecer, rojizo al recibir un golpe.
        if (Time.time < activeAt)
        {
            Color c = baseColor;
            c.a = 0.4f;
            sprite.color = c;
        }
        else
        {
            sprite.color = Time.time < flashUntil ? new Color(1f, 0.45f, 0.45f) : baseColor;
        }

        if (!Playing || Time.time < activeAt || dead) return;

        Vector2 toPlayer = (Vector2)Player.Instance.transform.position - (Vector2)transform.position;

        if (kind == EnemyKind.Shooter && Time.time >= nextFire)
        {
            nextFire = Time.time + fireInterval;
            FireTear(toPlayer.normalized);
        }
        else if (kind == EnemyKind.Boss && Time.time >= nextFire)
        {
            bool enraged = Health < maxHealth * 0.5f; // en la segunda fase dispara más y más rápido
            nextFire = Time.time + (enraged ? fireInterval * 0.7f : fireInterval);
            FireRing(enraged ? 12 : 8);
        }
    }

    void FixedUpdate()
    {
        if (!Playing || Time.time < activeAt || dead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (Time.time < stunUntil) return; // dejamos actuar al empujón

        Vector2 desired = Vector2.zero;
        Vector2 toPlayer = (Vector2)Player.Instance.transform.position - rb.position;

        switch (kind)
        {
            case EnemyKind.Chaser:
            case EnemyKind.Boss:
                desired = toPlayer.normalized * speed;
                break;

            case EnemyKind.Shooter:
                if (Time.time >= nextWander)
                {
                    nextWander = Time.time + Random.Range(1.2f, 2.8f);
                    wanderDir = Random.insideUnitCircle.normalized;
                }
                desired = wanderDir * speed;
                break;
        }

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, acceleration * Time.fixedDeltaTime);
    }

    void FireTear(Vector2 dir)
    {
        GameObject go = Instantiate(tearPrefab, (Vector2)transform.position + dir * 0.5f, Quaternion.identity);
        go.GetComponent<Tear>().Launch(dir * tearSpeed, 1f, false, 12f);
        SoundManager.Instance.PlayShoot();
    }

    void FireRing(int count)
    {
        ringOffset += 15f; // el anillo gira un poco cada vez
        for (int i = 0; i < count; i++)
        {
            float angle = (ringOffset + i * (360f / count)) * Mathf.Deg2Rad;
            FireTear(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        }
    }

    /// <summary>Recibe daño de una lágrima del jugador; se empuja en la dirección del disparo.</summary>
    public void TakeDamage(float amount, Vector2 knockbackDir)
    {
        if (dead || Time.time < activeAt) return;

        Health -= amount;
        flashUntil = Time.time + 0.1f;
        stunUntil = Time.time + 0.08f;
        rb.AddForce(knockbackDir * (kind == EnemyKind.Boss ? 1f : 4f), ForceMode2D.Impulse);
        SoundManager.Instance.PlayEnemyHit();

        if (Health <= 0f) Die();
    }

    void Die()
    {
        dead = true;
        SoundManager.Instance.PlayEnemyDie();
        CameraShake.Kick(kind == EnemyKind.Boss ? 0.4f : 0.12f);
        GameController.Instance.RegisterKill(scoreValue);
        FloorManager.Instance.OnEnemyKilled(this);
        Destroy(gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (dead || !Playing || Time.time < activeAt) return;

        Player player = collision.collider.GetComponent<Player>();
        if (player != null) player.TakeDamage(contactDamage, transform.position);
    }
}
