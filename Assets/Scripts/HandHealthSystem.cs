using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class HandHealthSystem : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float startingHealth = 5f;
    [Tooltip("Seconds of invulnerability after taking a hit (0 = none).")]
    [SerializeField] private float invulnerabilityTime = 0f;

    [Header("Hand Animation")]
    [SerializeField] private VideoClip handVideo;
    [SerializeField] private RawImage display;
    [Tooltip("How quickly the hand animation catches up to the real health value.")]
    [SerializeField] private float animationSmoothing = 8f;
    [Tooltip("Tick if your video goes from damaged -> healthy instead of healthy -> damaged.")]
    [SerializeField] private bool invertVideo = false;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDamaged;
    public event Action OnHealed;
    public event Action OnDeath;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;
    public bool IsDead { get; private set; }

    private VideoPlayer player;
    private RenderTexture renderTexture;
    private bool videoReady;
    private bool isSeeking;
    private long lastFrameSet = -1;
    private float displayedPercent = 1f;
    private float invulnerableUntil;

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();
        CurrentHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
        displayedPercent = HealthPercent;
        SetupVideo();
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.prepareCompleted -= OnPrepared;
            player.seekCompleted -= OnSeekCompleted;
        }
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Update()
    {
        if (!videoReady) return;

        displayedPercent = Mathf.Lerp(
            displayedPercent,
            HealthPercent,
            1f - Mathf.Exp(-animationSmoothing * Time.deltaTime));

        if (Mathf.Abs(displayedPercent - HealthPercent) < 0.0005f)
            displayedPercent = HealthPercent;

        ApplyFrame(displayedPercent);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        if (Time.time < invulnerableUntil) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        invulnerableUntil = Time.time + invulnerabilityTime;

        OnDamaged?.Invoke();
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);

        OnHealed?.Invoke();
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetHealth(float value)
    {
        CurrentHealth = Mathf.Clamp(value, 0f, maxHealth);
        IsDead = CurrentHealth <= 0f;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void Revive(float healthAmount = -1f)
    {
        IsDead = false;
        CurrentHealth = healthAmount < 0f ? maxHealth : Mathf.Clamp(healthAmount, 1f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void SetupVideo()
    {
        if (handVideo == null)
        {
            Debug.LogError("HandHealthSystem: no Hand Video assigned.", this);
            return;
        }

        renderTexture = new RenderTexture((int)handVideo.width, (int)handVideo.height, 0);
        renderTexture.Create();

        player.playOnAwake = false;
        player.isLooping = false;
        player.skipOnDrop = false;
        player.waitForFirstFrame = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.source = VideoSource.VideoClip;
        player.clip = handVideo;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = renderTexture;

        if (display != null)
            display.texture = renderTexture;

        player.prepareCompleted += OnPrepared;
        player.seekCompleted += OnSeekCompleted;
        player.Prepare();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
        vp.Pause();
        videoReady = true;
        lastFrameSet = -1;
        ApplyFrame(displayedPercent);
    }

    private void OnSeekCompleted(VideoPlayer vp)
    {
        isSeeking = false;
    }

    private void ApplyFrame(float percent)
    {
        if (isSeeking) return;

        long lastFrame = (long)player.frameCount - 1;
        if (lastFrame <= 0) return;

        float t = invertVideo ? percent : 1f - percent;
        long targetFrame = (long)Mathf.Round(Mathf.Clamp01(t) * lastFrame);

        if (targetFrame == lastFrameSet) return;

        lastFrameSet = targetFrame;
        isSeeking = true;
        player.frame = targetFrame;
    }

#if UNITY_EDITOR
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.D)) TakeDamage(10f);
        if (Input.GetKeyDown(KeyCode.H)) Heal(10f);
    }
#endif
}