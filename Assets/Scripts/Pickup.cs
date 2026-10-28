using UnityEngine;

public enum PickupKind
{
    Heart,
    DamageUp,
    RateUp,
    SpeedUp
}

/// <summary>
/// Objeto recogible: corazón (cura) o mejora (daño, cadencia, velocidad).
/// Flota suavemente y se aplica al tocar al jugador.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    public PickupKind kind;
    [Tooltip("Si es el objeto del cuarto del tesoro, al recogerlo se marca como tomado.")]
    public bool isRoomItem;

    Vector3 basePosition;
    float phase;

    /// <summary>Configura el tipo, el sprite y si es el objeto del cuarto del tesoro.</summary>
    public void Setup(PickupKind pickupKind, Sprite icon, bool roomItem)
    {
        kind = pickupKind;
        isRoomItem = roomItem;
        GetComponent<SpriteRenderer>().sprite = icon;
    }

    void Start()
    {
        basePosition = transform.position;
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f + phase) * 0.08f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player == null || !player.Collect(kind)) return;

        SoundManager.Instance.PlayPickup();
        if (isRoomItem) FloorManager.Instance.MarkItemTaken();
        Destroy(gameObject);
    }
}
