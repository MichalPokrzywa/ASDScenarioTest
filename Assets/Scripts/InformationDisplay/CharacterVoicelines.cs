using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterVoicelines", menuName = "Audio/Character Voicelines", order = 1)]
public class CharacterVoicelines : ScriptableObject
{
    [Header("Character Info")]
    public string characterName;

    [Header("Voicelines")]
    public List<VoicelineEntry> voicelines = new List<VoicelineEntry>();

    // Get voicelines by category
    public List<VoicelineEntry> GetVoicelines(string category)
    {
        return voicelines.FindAll(v => v.voicelineName.Contains(category));
    }

    public VoicelineEntry FindVoiceline(string name)
    {
        return voicelines.Find(v => v.voicelineName == name);
    }
}
[System.Serializable]
public class VoicelineEntry
{
    [Tooltip("Description of what this line contains")]
    [TextArea(2, 4)]
    public string voicelineName;

    [Tooltip("The audio clip to play")]
    public AudioClip audioClip;

}
