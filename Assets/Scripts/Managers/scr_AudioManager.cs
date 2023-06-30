using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scr_AudioManager: MonoBehaviour
{
    public static scr_AudioManager instance = null;

    private AudioSource musicAudioSource;
    private float musicVolumeScale;
    public string currentMusic;

    [Header("Sounds")]
    [SerializeField] private SoundAudioClip[] soundAudioClipArray;

    [Header("Music")]
    [SerializeField] private MusicAudioClip[] musicAudioClipArray;

    [System.Serializable]
    public class SoundAudioClip
    {
        public string sound;
        public AudioClip audioClip;
    }

    [System.Serializable]
    public class MusicAudioClip
    {
        public string music;
        public AudioClip audioClip;
    }

    private void Awake()
    {
        musicAudioSource = GetComponent<AudioSource>();
        musicVolumeScale = 1f;

        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayMusic("Default");
    }

    public void UpdateVolume()
    {
        if (musicAudioSource == null)
        {
            musicAudioSource = GetComponent<AudioSource>();
        }

        musicAudioSource.volume = MenuController.soundVolume * musicVolumeScale;
    }

    public void PlaySound(string sound, GameObject gameObject, float volumeScale = 1f)
    {
        string name = sound.ToString() + "Sound";
        AudioClip audioClip = GetSoundAudioClip(sound);

        if (audioClip != null)
        {
            GameObject soundGameObject = new GameObject(name);
            soundGameObject.transform.SetParent(gameObject.transform);
            AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();
            audioSource.PlayOneShot(audioClip, MenuController.soundVolume * volumeScale);

            if (soundGameObject != null)
            {
                Destroy(soundGameObject, audioClip.length);
            }
        }
    }

    public void PlaySoundAtPosition(string sound, Vector3 pos, float volumeScale = 1f)
    {
        string name = sound.ToString() + "Sound";
        AudioClip audioClip = GetSoundAudioClip(sound);

        if (audioClip != null)
        {
            GameObject soundGameObject = new GameObject(name);
            soundGameObject.transform.position = pos;
            AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();
            audioSource.PlayOneShot(audioClip, MenuController.soundVolume * volumeScale);
            
            if (soundGameObject != null)
            {
                Destroy(soundGameObject, audioClip.length);
            }
        }
    }

    public void PlayRepeatSound(string sound, GameObject gameObject, float interval, int numberOfTimes = 0, float volumeScale = 1f)
    {
        string name = sound.ToString() + "Sound";
        AudioClip audioClip = GetSoundAudioClip(sound);

        if (audioClip != null)
        {
            GameObject soundGameObject = new GameObject(name);
            soundGameObject.transform.SetParent(gameObject.transform);
            AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();

            float cooldown = audioClip.length + interval;
            instance.StartCoroutine(Repeat(cooldown, audioSource, audioClip, soundGameObject, numberOfTimes, volumeScale));
        }
    }

    public void StopRepeatSound(string sound, GameObject gameObject)
    {
        string name = sound.ToString() + "Sound";
        Transform soundTransform = gameObject.transform.Find(name);

        if (soundTransform != null)
        {
            Destroy(soundTransform.gameObject);
        }
    }

    public void PlayMusic(string music, float volumeScale = 1f)
    {
        if (currentMusic != music)
        {
            AudioClip audioClip = GetMusicAudioClip(music);

            if (audioClip != null)
            {
                musicAudioSource.Stop();
                musicVolumeScale = volumeScale;
                musicAudioSource.volume = MenuController.soundVolume * musicVolumeScale;
                musicAudioSource.clip = audioClip;
                musicAudioSource.Play();
                currentMusic = music;
            }
        }
    }

    private AudioClip GetSoundAudioClip(string sound)
    {
        foreach (SoundAudioClip soundAudioClip in instance.soundAudioClipArray)
        {
            if (soundAudioClip.sound == sound)
            {
                return soundAudioClip.audioClip;
            }
        }

        Debug.LogError("Sound " + sound + " not found");
        return null;
    }

    private AudioClip GetMusicAudioClip(string music)
    {
        foreach (MusicAudioClip musicAudioClip in instance.musicAudioClipArray)
        {
            if (musicAudioClip.music == music)
            {
                return musicAudioClip.audioClip;
            }
        }

        Debug.LogError("Music " + music + " not found");
        return null;
    }

    private IEnumerator Repeat(float interval, AudioSource audioSource, AudioClip audionClip, GameObject soundGameObject, int numberOfTimes, float volumeScale)
    {
        int count = 0;

        while (soundGameObject != null)
        {
            audioSource.PlayOneShot(audionClip, MenuController.soundVolume * volumeScale);
            yield return new WaitForSeconds(interval);

            if (numberOfTimes != 0)
            {
                count++;
            }

            if (numberOfTimes != 0 && count == numberOfTimes)
            {
                if (soundGameObject != null)
                {
                    Destroy(soundGameObject);
                }

                break;
            }    
        }
    }

}
