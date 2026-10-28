using UnityEngine;

/// <summary>
/// Lágrima (proyectil). La usan el jugador y los enemigos: el campo
/// "fromPlayer" decide a quién daña. Desaparece al chocar con paredes o
/// rocas, o al agotar su alcance.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Tear : MonoBehaviour
{
    float damage;
    bool fromPlayer;
    float dieAt;
    Rigidbody2D rb;

    /// <summary>Dispara la lágrima con una velocidad, daño y alcance.</summary>
    public void Launch(Vector2 velocity, float tearDamage, bool playerTear, float range)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = velocity;
        damage = tearDamage;
        fromPlayer = playerTear;
        dieAt = Time.time + range / Mathf.Max(0.1f, velocity.magnitude);
    }

    void Update()
    {
        bool playing = GameController.Instance != null && GameController.Instance.State == GameState.Playing;
        if (!playing || Time.time >= dieAt) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignora puertas, objetos y otras lágrimas (todos son triggers).
        if (other.isTrigger) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            if (!fromPlayer) return; // las lágrimas enemigas atraviesan a otros enemigos
            enemy.TakeDamage(damage, rb.linearVelocity.normalized);
            Destroy(gameObject);
            return;
        }

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            if (fromPlayer) return; // tu propia lágrima no te daña
            player.TakeDamage(1, transform.position);
            Destroy(gameObject);
            return;
        }

        // Pared o roca.
        Destroy(gameObject);
    }
}
