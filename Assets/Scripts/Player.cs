using UnityEngine;

/// <summary>
/// Jugador: se mueve con WASD, dispara lágrimas con las flechas, tiene vida
/// (en mitades de corazón) y mejoras acumulables. El movimiento usa la
/// velocidad del Rigidbody2D, así que choca de verdad con paredes y rocas.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    [Header("Vida (cada corazón = 2 puntos)")]
    public int maxHealth = 6;
    public float invulnerableSeconds = 1f;

    [Header("Movimiento")]
    public float moveSpeed = 6f;
    public float acceleration = 70f;

    [Header("Disparo")]
    public GameObject tearPrefab;
    [Tooltip("Disparos por segundo.")]
    public float fireRate = 2.5f;
    public float damage = 3.5f;
    public float tearSpeed = 9f;
    [Tooltip("Distancia máxima que recorre la lágrima.")]
    public float tearRange = 7f;

    public int Health { get; private set; }
    public float Damage { get { return damage; } }
    public float FireRate { get { return fireRate; } }
    public float MoveSpeed { get { return moveSpeed; } }
    public bool IsInvulnerable { get { return Time.time < invulnerableUntil; } }

    Rigidbody2D rb;
    SpriteRenderer sprite;
    float invulnerableUntil;
    float stunUntil;
    float nextShot;
    float baseMove, baseRate, baseDamage;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        baseMove = moveSpeed;
        baseRate = fireRate;
        baseDamage = damage;
        Health = maxHealth;
    }

    bool Playing
    {
        get { return GameController.Instance != null && GameController.Instance.State == GameState.Playing; }
    }

    void Update()
    {
        bool playing = Playing;

        // Parpadeo mientras dura la invulnerabilidad tras un golpe.
        bool blink = playing && IsInvulnerable && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
        Color c = sprite.color;
        c.a = blink ? 0.35f : 1f;
        sprite.color = c;

        if (!playing) return;

        Vector2 aim = Controls.Shoot;
        if (aim != Vector2.zero && Time.time >= nextShot)
        {
            Shoot(aim);
            nextShot = Time.time + 1f / fireRate;
        }
    }

    void FixedUpdate()
    {
        if (!Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (Time.time < stunUntil) return; // dejamos actuar al empujón del golpe

        Vector2 input = Controls.Move;
        if (input.sqrMagnitude > 1f) input.Normalize();

        // Aceleración suave hacia la velocidad deseada (sensación de peso).
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, input * moveSpeed,
                                                acceleration * Time.fixedDeltaTime);
    }

    void Shoot(Vector2 aim)
    {
        Vector2 dir = aim.normalized;
        Vector2 origin = (Vector2)transform.position + dir * 0.5f;
        GameObject go = Instantiate(tearPrefab, origin, Quaternion.identity);

        // La lágrima hereda un poco de la velocidad del jugador, como en el original.
        Vector2 velocity = dir * tearSpeed + rb.linearVelocity * 0.4f;
        go.GetComponent<Tear>().Launch(velocity, damage, true, tearRange);
        SoundManager.Instance.PlayShoot();
    }

    /// <summary>Recibe daño (en puntos de vida) desde una posición; empuja y da invulnerabilidad.</summary>
    public void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (!Playing || IsInvulnerable) return;

        Health = Mathf.Max(0, Health - amount);
        invulnerableUntil = Time.time + invulnerableSeconds;
        stunUntil = Time.time + 0.12f;

        Vector2 away = ((Vector2)transform.position - sourcePosition).normalized;
        rb.linearVelocity = away * 7f;

        SoundManager.Instance.PlayHurt();
        CameraShake.Kick(0.25f);

        if (Health <= 0) GameController.Instance.PlayerDied();
    }

    /// <summary>Aplica un objeto recogido. Devuelve false si no se pudo usar (corazón con vida llena).</summary>
    public bool Collect(PickupKind kind)
    {
        switch (kind)
        {
            case PickupKind.Heart:
                if (Health >= maxHealth) return false;
                Health = Mathf.Min(maxHealth, Health + 2);
                return true;
            case PickupKind.DamageUp:
                damage += 1.2f;
                return true;
            case PickupKind.RateUp:
                fireRate += 0.6f;
                return true;
            case PickupKind.SpeedUp:
                moveSpeed += 0.8f;
                return true;
        }
        return false;
    }

    /// <summary>Coloca al jugador en una posición y anula su movimiento.</summary>
    public void Teleport(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Restaura vida y estadísticas al empezar una partida.</summary>
    public void ResetForRun()
    {
        Health = maxHealth;
        moveSpeed = baseMove;
        fireRate = baseRate;
        damage = baseDamage;
        invulnerableUntil = 0f;
        stunUntil = 0f;
        nextShot = 0f;
        Teleport(Vector2.zero);
    }
}
