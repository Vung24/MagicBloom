using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip pourSfx;
    [SerializeField] private AudioClip winSfx;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SourcesAudio();
    }

    private void SourcesAudio()
    {
        if(musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.6f;
        }

        if(sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
    }

    public void PlayBackground()
    {
        if(musicSource == null || backgroundMusic == null)
        {
            return;
        }

        if(musicSource.isPlaying && musicSource.clip == backgroundMusic)
        {
            return;
        }

        musicSource.clip = backgroundMusic;
        musicSource.Play();
    }

    public void StopBackground()
    {
        if(musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    public void PlayPour()
    {
        PlaySfx(pourSfx);
    }

    public void PlayWin()
    {
        PlaySfx(winSfx);
    }

    private void PlaySfx(AudioClip clip)
    {
        if(sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }
}
