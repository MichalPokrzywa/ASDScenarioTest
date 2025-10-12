using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VoicelineButton : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public Button playButton;
    public Image playButtonImage;

    [Header("Play/Stop Icons (Optional)")]
    public Sprite playIcon;
    public Sprite stopIcon;

    private VoicelineEntry _voiceline;
    private AudioSource _audioSource;
    private bool _isPlaying = false;

    public void Initialize(VoicelineEntry voiceline, AudioSource audioSource)
    {
        _voiceline = voiceline;
        _audioSource = audioSource;

        if (nameText != null)
            nameText.text = voiceline.voicelineName;

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        UpdateButtonVisual(false);
    }

    private void OnPlayButtonClicked()
    {
        if (_isPlaying)
        {
            StopVoiceline();
        }
        else
        {
            PlayVoiceline();
        }
    }

    private void PlayVoiceline()
    {
        if (_voiceline.audioClip == null || _audioSource == null) return;

        _audioSource.clip = _voiceline.audioClip;
        _audioSource.Play();
        _isPlaying = true;

        UpdateButtonVisual(true);

        StartCoroutine(WaitForAudioEnd());
    }

    private void StopVoiceline()
    {
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }
        _isPlaying = false;
        UpdateButtonVisual(false);
    }

    private IEnumerator WaitForAudioEnd()
    {
        yield return new WaitWhile(() => _audioSource.isPlaying);
        _isPlaying = false;
        UpdateButtonVisual(false);
    }

    private void UpdateButtonVisual(bool isPlaying)
    {
        if (playButtonImage != null)
        {
            if (isPlaying && stopIcon != null)
            {
                playButtonImage.sprite = stopIcon;
            }
            else if (!isPlaying && playIcon != null)
            {
                playButtonImage.sprite = playIcon;
            }
        }

        if (playButton != null)
        {
            var colors = playButton.colors;
            colors.normalColor = isPlaying ? new Color(1f, 0.5f, 0.5f) : Color.white;
            playButton.colors = colors;
        }
    }

    public void ForceStop()
    {
        StopVoiceline();
    }
}
