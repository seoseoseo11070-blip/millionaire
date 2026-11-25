using UnityEngine;

public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    [Header("AudioSource")]
    [SerializeField] private AudioSource seSource;

    [Header("カード・進行")]
    [SerializeField] private AudioClip cardPlay;
    [SerializeField] private AudioClip cardDeal;
    [SerializeField] private AudioClip pass;
    [SerializeField] private AudioClip turnStart;

    [Header("特殊")]
    [SerializeField] private AudioClip shibari;
    [SerializeField] private AudioClip revolution;
    [SerializeField] private AudioClip eightCut;
    [SerializeField] private AudioClip sevenGive;
    [SerializeField] private AudioClip finish;
    [SerializeField] private AudioClip fieldClear;

    [Header("UI")]
    [SerializeField] private AudioClip uiClick;
    [SerializeField] private AudioClip panelOpen;
    [SerializeField] private AudioClip victoryRank;
    [SerializeField] private AudioClip victoryJingle;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (seSource == null)
            seSource = GetComponent<AudioSource>();
        if (seSource == null)
            seSource = gameObject.AddComponent<AudioSource>();

        seSource.playOnAwake = false;
        seSource.loop = false;
        seSource.spatialBlend = 0f;
    }

    public void Play(AudioClip clip, float volScale = 1f)
    {
        if (clip == null || seSource == null) return;
        seSource.PlayOneShot(clip, Mathf.Clamp01(volume * volScale));
    }

    public void PlayCardPlay() => Play(cardPlay);
    public void PlayCardDeal() => Play(cardDeal, 0.7f);
    public void PlayPass() => Play(pass);
    public void PlayTurnStart() => Play(turnStart, 0.5f);
    public void PlayShibari() => Play(shibari);
    public void PlayRevolution() => Play(revolution);
    public void PlayEightCut() => Play(eightCut);
    public void PlaySevenGive() => Play(sevenGive);
    public void PlayFinish() => Play(finish);
    public void PlayFieldClear() => Play(fieldClear);
    public void PlayUiClick() => Play(uiClick, 0.8f);
    public void PlayPanelOpen() => Play(panelOpen, 0.8f);
    public void PlayVictoryRank() => Play(victoryRank);
    public void PlayVictoryJingle() => Play(victoryJingle);
}