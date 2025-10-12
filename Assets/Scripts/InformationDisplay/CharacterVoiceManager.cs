using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterVoiceManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterVoicelines voicelinesData;
    [SerializeField] private AudioSource audioSource;

    [Header("UI References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject voicelineButtonPrefab;

    [Header("Search (Optional)")]
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private bool enableSearch = true;

    private List<VoicelineButton> _instantiatedButtons = new List<VoicelineButton>();
    private string _searchQuery = "";

    void Start()
    {
        // Create audio source if not assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Initialize UI
        if (voicelinesData != null)
        {
            InitializeUI();
        }
        else
        {
            Debug.LogError("No CharacterVoicelines assigned to VoicelineUIManager!");
        }
    }

    private void InitializeUI()
    {
        // Setup search
        if (enableSearch && searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchChanged);
        }

        // Generate initial buttons
        GenerateVoicelineButtons();
    }

    private void OnSearchChanged(string query)
    {
        _searchQuery = query.ToLower();
        foreach (var button in _instantiatedButtons)
        {
            button.gameObject.SetActive(button.nameText.text.ToLower().Contains(query,StringComparison.InvariantCultureIgnoreCase));
        }
    }

    private void GenerateVoicelineButtons()
    {
        ClearButtons();

        foreach (var voiceline in voicelinesData.voicelines)
        {
            CreateVoicelineButton(voiceline);
        }
    }

    private void CreateVoicelineButton(VoicelineEntry voiceline)
    {
        if (voicelineButtonPrefab == null || contentContainer == null) return;

        GameObject buttonObj = Instantiate(voicelineButtonPrefab, contentContainer);
        VoicelineButton button = buttonObj.GetComponent<VoicelineButton>();

        if (button != null)
        {
            button.Initialize(voiceline, audioSource);
            _instantiatedButtons.Add(button);
        }
    }

    private void ClearButtons()
    {
        foreach (var button in _instantiatedButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        _instantiatedButtons.Clear();
    }

    // Public method to change character data at runtime
    public void LoadCharacterVoicelines(CharacterVoicelines newData)
    {
        voicelinesData = newData;
        InitializeUI();
    }

    // Public method to stop all playing voicelines
    public void StopAllVoicelines()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        foreach (var button in _instantiatedButtons)
        {
            button.ForceStop();
        }
    }
}
