using System;
using UnityEngine;

public class DataManager
{
    #region Player Prefs Operations

    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public static void SaveData(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public static void SaveData(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public static void SaveData(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public static void SaveData(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static T LoadData<T>(string key, T defaultValue = default)
    {
        if (typeof(T) == typeof(int))
        {
            int intValue = PlayerPrefs.GetInt(key, Convert.ToInt32(defaultValue));
            return (T)Convert.ChangeType(intValue, typeof(T));
        }
        else if (typeof(T) == typeof(float))
        {
            float floatValue = PlayerPrefs.GetFloat(key, Convert.ToSingle(defaultValue));
            return (T)Convert.ChangeType(floatValue, typeof(T));
        }
        else if (typeof(T) == typeof(string))
        {
            string stringValue = PlayerPrefs.GetString(key, Convert.ToString(defaultValue));
            return (T)Convert.ChangeType(stringValue, typeof(T));
        }
        else if (typeof(T) == typeof(bool))
        {
            int intValue = PlayerPrefs.GetInt(key, defaultValue != null && (bool)(object)defaultValue ? 1 : 0);
            return (T)Convert.ChangeType(intValue == 1, typeof(T));
        }
        else
        {
            throw new NotSupportedException("Unsupported data type");
        }
    }

    #endregion
}