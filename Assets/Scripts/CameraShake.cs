using UnityEngine;

/// <summary>Sacudida de cámara breve para dar "peso" a golpes y muertes.</summary>
public class CameraShake : MonoBehaviour
{
    static CameraShake instance;

    float intensity;
    Vector3 basePosition;

    void Awake()
    {
        instance = this;
        basePosition = transform.position;
    }

    /// <summary>Pide una sacudida (0.1 = leve, 0.4 = fuerte).</summary>
    public static void Kick(float amount)
    {
        if (instance != null) instance.intensity = Mathf.Max(instance.intensity, amount);
    }

    void LateUpdate()
    {
        intensity = Mathf.MoveTowards(intensity, 0f, Time.deltaTime * 1.2f);
        Vector2 offset = Random.insideUnitCircle * intensity;
        transform.position = basePosition + new Vector3(offset.x, offset.y, 0f);
    }
}
