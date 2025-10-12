using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Audio Storage")]
    public AudioStorage audioStorage;

    [Space]
    public float musicVolume = 0.5f;
    public float soundsVolume = 0.5f;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SoundVolumeKey = "SoundVolume";

    [Header("Audio Sources")]
    public AudioSource musicAudioSource;
    public AudioSource soundsAudioSource;

    [Header("Audio Mixer Groups")]
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup soundsGroup;

    private void Awake()
    {
        LoadVolumes();
        PlayMenuMusic();
    }

    private void LoadVolumes()
    {
        musicVolume = DataManager.LoadData<float>(MusicVolumeKey, -20f);
        soundsVolume = DataManager.LoadData<float>(SoundVolumeKey, -20f);

        // Apply to AudioMixer
        audioMixer.SetFloat("Music", musicVolume);
        audioMixer.SetFloat("Sound", soundsVolume);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        DataManager.SaveData(MusicVolumeKey, musicVolume);
        audioMixer.SetFloat("Music", musicVolume);

        musicAudioSource.volume = volume;
    }

    public void SetSoundVolume(float volume)
    {
        soundsVolume = volume;
        DataManager.SaveData(SoundVolumeKey, soundsVolume);
        audioMixer.SetFloat("Sound", soundsVolume);

        soundsAudioSource.volume = volume;
    }

    public void PlayMenuMusic()
    {
        musicAudioSource.clip = audioStorage.GetRandomMenuMusic();
        musicAudioSource.Play();
    }

    public void PlaySound(Sound sound)
    {
        AudioClip clip = audioStorage.GetSoundByType(sound);
        if (clip == null) return;

        soundsAudioSource.PlayOneShot(clip);
    }

    public void PlaySoundAttachedToPlayer(AudioClip clip, GameObject parent, float volume = 1.0f)
    {
        if (clip == null || parent == null) return;

        GameObject audioObject = new GameObject("PlayerSound");
        audioObject.transform.SetParent(parent.transform);
        audioObject.transform.localPosition = Vector3.zero;

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = soundsGroup;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();

        Destroy(audioObject, clip.length);
    }
}