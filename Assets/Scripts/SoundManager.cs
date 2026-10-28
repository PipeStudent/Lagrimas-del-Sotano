using UnityEngine;

/// <summary>
/// Música de fondo (BGM, en bucle) y efectos de sonido (SFX). Cada efecto
/// tiene un volumen relativo propio para lograr una mezcla equilibrada: la
/// música queda por debajo y baja aún más en las pantallas de fin.
/// Tecla M: silenciar / activar.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Clips")]
    public AudioClip bgm;
    public AudioClip shoot;
    public AudioClip enemyHit;
    public AudioClip enemyDie;
    public AudioClip hurt;
    public AudioClip pickup;
    public AudioClip door;
    public AudioClip win;
    public AudioClip lose;

    [Header("Mezcla")]
    [Range(0f, 1f)] public float bgmVolume = 0.30f;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;
    [Range(0f, 1f)] public float duckedFactor = 0.25f;
    [Tooltip("Variación aleatoria de tono para que los efectos repetidos no suenen idénticos.")]
    public float pitchVariation = 0.08f;

    AudioSource bgmSource;
    AudioSource sfxSource;
    bool muted;

    void Awake()
    {
        Instance = this;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.clip = bgm;
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    void Start()
    {
        if (bgm != null) bgmSource.Play();
    }

    void Update()
    {
        if (Controls.MutePressed)
        {
            muted = !muted;
            AudioListener.volume = muted ? 0f : 1f;
        }
    }

    /// <summary>Baja la música durante las pantallas de victoria / derrota.</summary>
    public void DuckMusic(bool duck)
    {
        bgmSource.volume = bgmVolume * (duck ? duckedFactor : 1f);
    }

    // Cada evento del juego dispara su efecto con un volumen relativo propio.
    public void PlayShoot()    { Play(shoot, 0.45f, true); }
    public void PlayEnemyHit() { Play(enemyHit, 0.70f, true); }
    public void PlayEnemyDie() { Play(enemyDie, 0.85f, true); }
    public void PlayHurt()     { Play(hurt, 1.00f, false); }
    public void PlayPickup()   { Play(pickup, 0.80f, false); }
    public void PlayDoor()     { Play(door, 0.70f, false); }
    public void PlayWin()      { Play(win, 0.90f, false); }
    public void PlayLose()     { Play(lose, 0.90f, false); }

    void Play(AudioClip clip, float relativeVolume, bool varyPitch)
    {
        if (clip == null) return;
        sfxSource.pitch = varyPitch ? 1f + Random.Range(-pitchVariation, pitchVariation) : 1f;
        sfxSource.PlayOneShot(clip, sfxVolume * relativeVolume);
    }
}
