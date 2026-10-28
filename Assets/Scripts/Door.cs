using UnityEngine;

/// <summary>
/// Puerta de la sala. Cerrada mientras queden enemigos; al abrirse, si el
/// jugador la toca, lo lleva a la sala vecina (FloorManager se encarga).
/// Direcciones: 0 = arriba, 1 = derecha, 2 = abajo, 3 = izquierda.
/// </summary>
public class Door : MonoBehaviour
{
    public int direction;
    public Sprite openSprite;
    public Sprite closedSprite;

    SpriteRenderer sr;
    bool open;

    /// <summary>"exists": hay sala al otro lado. "isOpen": se puede cruzar.</summary>
    public void SetState(bool exists, bool isOpen)
    {
        gameObject.SetActive(exists);
        if (!exists) return;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        open = isOpen;
        sr.sprite = isOpen ? openSprite : closedSprite;
    }

    // Stay (no Enter) para que funcione aunque el jugador ya esté sobre la puerta al abrirse.
    void OnTriggerStay2D(Collider2D other)
    {
        if (!open) return;
        if (other.GetComponent<Player>() != null) FloorManager.Instance.TryMove(direction);
    }
}
