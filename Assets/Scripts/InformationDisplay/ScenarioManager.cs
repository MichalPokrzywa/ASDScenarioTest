using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnitEye;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{

    private AudioClip clip;
    private string micName;
    private int sampleRate = 16000;
    private bool recording = false;
    private string outputPath;
    public SubjectCanvas subjectCanvas;
    public Gaze gaze;
    public TMP_Text blinkingText;
    public TMP_Text drowsinessText;
    public TMP_Text calibrationText;
    public TMP_Text distanceText;
    public void Update()
    {
        blinkingText.text = gaze.Blinking ? "Eyes are closed" : "Eyes are open";
        drowsinessText.text = gaze.EyeHelper.Calibrating ? $"Calibrating Drowsiness based on {gaze.EyeHelper.CalibrationCount} values" : "Calibrate Drowsiness Baseline";
        calibrationText.text = gaze.CalibrationScript ? gaze.CalibrationScript.GUIMessage :"Calibration not running";
        distanceText.text = $"Calculated distance: {gaze.Distance:F1} mm";
    }

    public void StartRecording()
    {
        micName = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (micName == null) { Debug.LogError("Brak mikrofonu"); return; }
        subjectCanvas.StartTask();
        string recordingsDir = $"{Application.dataPath}/Recordings";
        if (!Directory.Exists(recordingsDir))
        {
            Directory.CreateDirectory(recordingsDir);
        }
        // Use safe filename format (no colons)
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        outputPath = $"{recordingsDir}/{timestamp}.wav";
        clip = Microphone.Start(micName, false, 3599, sampleRate); 
        gaze.PauseCSVLogging = false;
        gaze.ASDAnalistManager.ResetSession();
        recording = true;
        Debug.Log("Recording started...");
    }

    public void StopRecording()
    {
        if (!recording) return;
        int length = Microphone.GetPosition(micName);
        Microphone.End(micName);
        subjectCanvas.StopTask();
        gaze.PauseCSVLogging = true;
        float[] samples = new float[length * clip.channels];
        clip.GetData(samples, 0);
        SaveWav(outputPath, samples, clip.channels, sampleRate);
        recording = false;
        Debug.Log($"Recording saved to {outputPath}");
        string praatOutput = $"{DateTime.Now.ToString("yyyy - MM - dd_HH - mm - ss")}.csv";
        if (Praat.RunPraatScript(outputPath, praatOutput))
        {
            gaze.ASDAnalistManager.LoadAudioAnalysis($"{Application.dataPath}/Praat/{praatOutput}");
            string output = $"{Application.dataPath}/Results";
            if (!Directory.Exists(output))
            {
                Directory.CreateDirectory(output);
            }
            gaze.ASDAnalistManager.ExportResults($"{output}");
        }
    }

    public void RestartScenario()
    {
        recording = false;
        gaze.PauseCSVLogging = true;
        gaze.ASDAnalistManager.ResetSession();
        subjectCanvas.RestartTask();
        clip = null;
    }

    public void StartCalibration()
    {
        subjectCanvas.ShowCalibration();
        gaze.LoadCalibration();
    }

    public void StopCalibration()
    {
        if (!gaze.GetComponent<GazeCalibration>().Finished) return;

        subjectCanvas.HideCalibration();
        gaze.UnloadCalibration();

    }

    public void QuitApplication()
    {
        Application.Quit();
    }

    public void BlinkCalibration()
    {
        gaze.EyeHelper.CalibrateBlinking();
        Debug.Log(gaze.EyeHelper.BlinkingThreshold);
    }

    public void DrowsyCalibration()
    {
        gaze.EyeHelper.CalibrateDrowsyStats(true);
    }

    public void DistanceCalibration()
    {
        gaze.EyeHelper.CameraFOV = 77f;
        gaze.EyeHelper.CalibrateFocalLength();
    }

    // Very simple WAV writer (32-bit Float mono)
    void SaveWav(string filename, float[] samples, int channels, int sampleRate)
    {
        using (var fs = new FileStream(filename, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * channels * 4;
            int blockAlign = channels * 4;
            int dataSize = samples.Length * 4;

            // RIFF header
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize); // file size - 8
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // fmt chunk size
            bw.Write((short)3); // IEEE Float format
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)blockAlign);
            bw.Write((short)32); // bits per sample

            // data chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            // write samples
            foreach (var s in samples)
            {
                bw.Write(Mathf.Clamp(s, -1f, 1f));
            }
        }
    }
}
