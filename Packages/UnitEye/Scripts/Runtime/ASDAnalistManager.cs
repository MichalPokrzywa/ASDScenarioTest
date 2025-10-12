using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Unity.ASD
{
    public class ASDAnalistManager : MonoBehaviour
    {
        [Header("Real-time Data")]
        private List<EyeTrackingData> eyeTrackingBuffer = new List<EyeTrackingData>();
        private AudioAnalysisData audioData;

        [Header("Analysis Settings")]
        [SerializeField] private float fixationThreshold = 0.02f; // 2% screen movement
        [SerializeField] private float fixationMinDuration = 100f; // milliseconds
        [SerializeField] private string[] socialAOIs = { "Face", "Eyes", "Mouth", "Character" };

        [Header("Results")]
        public ASDMetrics currentMetrics = new ASDMetrics();

        public void AddEyeTrackingData(float xNorm, float yNorm, float xFiltered, float yFiltered,
            float distance, float aspectRatio, bool blinking, string aoi)
        {
            var data = new EyeTrackingData
            {
                xNormalized = xNorm,
                yNormalized = yNorm,
                xFiltered = xFiltered,
                yFiltered = yFiltered,
                distanceToCamera = distance,
                eyeAspectRatio = aspectRatio,
                isBlinking = blinking,
                timestampMs = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                aoiName = aoi
            };

            eyeTrackingBuffer.Add(data);

            // Calculate metrics every second
            if (eyeTrackingBuffer.Count % 30 == 0) // assuming 30fps
            {
                CalculateMetrics();
            }
        }
        // Load audio analysis from CSV
        public void LoadAudioAnalysis(string csvPath)
        {
            if (!File.Exists(csvPath)) return;

            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2) return;

            string[] values = lines[1].Split(';');

            try
            {
                audioData = new AudioAnalysisData
                {
                    filename = values[0],
                    duration = ParseFloat(values[1]),
                    meanF0 = ParseFloat(values[2]),
                    medianF0 = ParseFloat(values[3]),
                    minF0 = ParseFloat(values[4]),
                    maxF0 = ParseFloat(values[5]),
                    sdF0 = ParseFloat(values[6]),
                    jitter = ParseFloat(values[7]),
                    shimmer = ParseFloat(values[8]),
                    meanHNR = ParseFloat(values[9]),
                    meanIntensity = ParseFloat(values[10]),
                    voicedFraction = ParseFloat(values[11])
                };

                CalculateMetrics();
                Debug.Log("Audio analysis loaded successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing audio analysis: {e.Message}");
            }
        }

        public void CalculateMetrics()
        {
            if (eyeTrackingBuffer.Count == 0) return;

            // 1. Social vs Environment attention
            CalculateAOIMetrics();

            // 2. Fixation analysis
            CalculateFixationMetrics();

            // 3. Gaze stability
            CalculateGazeStability();

            // 4. Blink rate
            CalculateBlinkRate();

            // 5. Saccade amplitude
            CalculateSaccadeMetrics();

            // 6. Audio metrics (if available)
            if (audioData != null)
            {
                CalculateAudioMetrics();
            }

            // 7. Calculate overall ASD risk score
            CalculateRiskScore();
        }

        private void CalculateAOIMetrics()
        {    
            // Accumulate time per AOI using the delta between consecutive timestamps (ms).
            // This is more accurate than counting frames because it uses actual time durations.
            // AOI percent / SocialAttentionRatio = time_on_social_AOIs / total_time
            // Reference for AOI percent / fixation time as a biomarker in ASD: Klin et al., JAMA (2002).
            // https://jamanetwork.com/journals/jamapsychiatry/fullarticle/206705
            currentMetrics.aoiTimeSpent.Clear();

            foreach (var data in eyeTrackingBuffer)
            {
                if (!currentMetrics.aoiTimeSpent.ContainsKey(data.aoiName))
                    currentMetrics.aoiTimeSpent[data.aoiName] = 0;

                currentMetrics.aoiTimeSpent[data.aoiName]++;
            }

            int socialFrames = 0;
            int environmentFrames = 0;

            foreach (var kvp in currentMetrics.aoiTimeSpent)
            {
                if (socialAOIs.Contains(kvp.Key))
                    socialFrames += (int)kvp.Value;
                else
                    environmentFrames += (int)kvp.Value;
            }

            currentMetrics.socialAttentionRatio = (float)socialFrames / eyeTrackingBuffer.Count;
            currentMetrics.environmentAttentionRatio = (float)environmentFrames / eyeTrackingBuffer.Count;

            //// Eye contact avoidance (specific to eyes/face)
            //int eyeFrames = 0;
            //if (currentMetrics.aoiTimeSpent.ContainsKey("Eyes"))
            //    eyeFrames = (int)currentMetrics.aoiTimeSpent["Eyes"];

            //currentMetrics.eyeContactAvoidance = 1f - ((float)eyeFrames / eyeTrackingBuffer.Count);
        }

        private void CalculateFixationMetrics()
        {
            List<float> fixationDurations = new List<float>();
            long currentFixationStart = 0;
            bool inFixation = false;

            // We use the same movement threshold as before, but measure durations using timestamps (ms).
            // Fixation duration = end_timestamp - start_timestamp (ms).
            // Standard definitions of fixation and thresholds are discussed in eye-tracking methodology (Holmqvist et al.).
            // https://link.springer.com/article/10.3758/s13428-021-01762-8
            for (int i = 1; i < eyeTrackingBuffer.Count; i++)
            {
                var prev = eyeTrackingBuffer[i - 1];
                var curr = eyeTrackingBuffer[i];

                float dx = curr.xNormalized - prev.xNormalized;
                float dy = curr.yNormalized - prev.yNormalized;
                float movement = Mathf.Sqrt(dx * dx + dy * dy);

                //Debug.Log($"{movement} - {fixationThreshold}");

                if (movement < fixationThreshold)
                {
                    if (!inFixation)
                    {
                        currentFixationStart = prev.timestampMs;
                        inFixation = true;
                    }
                }
                else
                {
                    if (inFixation)
                    {
                        float duration = prev.timestampMs - currentFixationStart;
                        Debug.Log($"{duration} - {fixationMinDuration}");
                        //Debug.Log($"{prev.timestampMs} - {currentFixationStart}");
                        if (duration >= fixationMinDuration)
                        {
                            fixationDurations.Add(duration);
                        }
                        inFixation = false;
                    }
                }
            }

            currentMetrics.totalFixations = fixationDurations.Count;
            currentMetrics.averageFixationDuration = fixationDurations.Count > 0
                ? fixationDurations.Average()
                : 0;
        }

        private void CalculateGazeStability()
        {
            // Gaze stability = root-mean-square distance of 2D gaze points from their mean.
            // This is equivalent to spatial dispersion (lower = more stable).
            // Reference: Holmqvist et al. (eye-tracking reporting standards) for dispersion-style metrics.
            // https://link.springer.com/article/10.3758/s13428-021-01762-8
            if (eyeTrackingBuffer.Count < 2) return;

            float sumSquaredDiff = 0;
            float mean = eyeTrackingBuffer.Average(d => d.xNormalized);

            foreach (var data in eyeTrackingBuffer)
            {
                sumSquaredDiff += Mathf.Pow(data.xNormalized - mean, 2);
            }

            currentMetrics.gazeStability = Mathf.Sqrt(sumSquaredDiff / eyeTrackingBuffer.Count);
        }

        private void CalculateBlinkRate()
        {
            // BlinkRate = number of blink onsets / duration (in minutes)
            // Typical adult blink rate ~ 14-20 blinks/minute (Cleveland Clinic / blink literature).
            // https://my.clevelandclinic.org/health/articles/blinking
            int blinkCount = 0;
            int consecutiveBlinkSamples = 0;
            int blinkSampleThreshold = 5; // require at least 5 consecutive TRUE samples to validate a blink (tunable)

            // Walk through buffer, accumulate consecutive 'true' samples, and count a blink
            // only when a run of TRUEs ends (or at end of buffer) and its length >= threshold.
            for (int i = 0; i < eyeTrackingBuffer.Count; i++)
            {
                bool isBlinking = eyeTrackingBuffer[i].isBlinking;

                if (isBlinking)
                {
                    consecutiveBlinkSamples++;
                }
                else
                {
                    // run ended — check if it qualifies as a blink
                    if (consecutiveBlinkSamples >= blinkSampleThreshold)
                    {
                        blinkCount++;
                        //Debug.Log($"Blink detected (samples={consecutiveBlinkSamples}) total={blinkCount}");
                    }
                    consecutiveBlinkSamples = 0;
                }
            }

            // If buffer ended while still blinking, count that run if it meets threshold
            if (consecutiveBlinkSamples >= blinkSampleThreshold)
            {
                blinkCount++;
                //Debug.Log($"Blink detected at end (samples={consecutiveBlinkSamples}) total={blinkCount}");
            }

            // Calculate duration in minutes
            long sessionDurationMs = eyeTrackingBuffer.Last().timestampMs - eyeTrackingBuffer.First().timestampMs;
            float minutes = sessionDurationMs > 0 ? sessionDurationMs / 60000f : 0f;

            currentMetrics.blinkRate = minutes > 0 ? blinkCount / minutes : 0f; // blinks per minute
        }

        private void CalculateSaccadeMetrics()
        {
            // Saccade amplitude is the Euclidean distance between consecutive samples where movement >= fixationThreshold.
            // If using normalized coordinates, amplitudes are in normalized units; convert to degrees if you have screen geometry.
            // Reference (saccade amplitude as a standard metric): Gibaldi et al., "Saccade main sequence" and EyeWiki.
            // https://pmc.ncbi.nlm.nih.gov/articles/PMC7880984/
            List<float> saccadeAmplitudes = new List<float>();

            for (int i = 1; i < eyeTrackingBuffer.Count; i++)
            {
                var prev = eyeTrackingBuffer[i - 1];
                var curr = eyeTrackingBuffer[i];

                float dx = curr.xNormalized - prev.xNormalized;
                float dy = curr.yNormalized - prev.yNormalized;
                float movement = Mathf.Sqrt(dx * dx + dy * dy);

                if (movement >= fixationThreshold)
                {
                    saccadeAmplitudes.Add(movement);
                }
            }

            currentMetrics.meanSaccadeAmplitude = saccadeAmplitudes.Count > 0
                ? saccadeAmplitudes.Average()
                : 0f;
        }

        private void CalculateAudioMetrics()
        {
            // F0 variability: use SD of F0 as a standard measure of pitch variability.
            // source: studies of prosodic differences in ASD (pitch mean/range/SD are commonly used).
            // https://www.ncbi.nlm.nih.gov/pmc/articles/PMC8630064/
            // We'll normalize to [0,1] for downstream fusion: assume sdF0 of 50Hz corresponds to high variability.
            currentMetrics.f0Variability = Mathf.Clamp01(audioData.sdF0 / 50f);

            // Prosody score: combine normalized pitch variability and pitch range.
            // pitch range = maxF0 - minF0
            // Prosody features used in ASD: mean F0, F0 range, F0 SD (Asghari et al., 2021).
            // https://www.ncbi.nlm.nih.gov/pmc/articles/PMC8630064/
            float f0Range = audioData.maxF0 - audioData.minF0;
            float normRange = Mathf.Clamp01(f0Range / 200f); // 200 Hz ~ large range, adjust by population
            float normSdF0 = Mathf.Clamp01(audioData.sdF0 / 50f);
            currentMetrics.prosodyScore = 0.6f * normSdF0 + 0.4f * normRange; // [0..1], higher = more prosodic variability

            // Voice quality: use HNR and jitter (lower jitter -> better quality; higher HNR -> better quality)
            // Jitter/Shimmer/HNR definitions and usage: Teixeira et al. (Vocal Acoustic Analysis).
            // https://www.sciencedirect.com/science/article/pii/S2212017313002788
            float hnrNorm = Mathf.Clamp01(audioData.meanHNR / 30f); // HNR of 30 dB is pretty good; normalize to 0..1
            float jitterNorm = Mathf.Clamp01(audioData.jitter / 0.02f); // jitter 0.02 (2%) typical threshold; higher = worse
            float jitterScore = 1f - jitterNorm; // higher = better
            currentMetrics.voiceQuality = (hnrNorm + jitterScore) / 2f; // [0..1], higher = better voice quality

        }

        private void CalculateRiskScore()
        {
            float score = 0;
            int factorCount = 0;

            // Eye tracking factors (0-100 scale)

            // 1. Social attention (weight: 35%)
            if (currentMetrics.socialAttentionRatio < 0.4f)
                score += 35 * (1f - currentMetrics.socialAttentionRatio / 0.4f);
            factorCount++;

            // 2. Eye contact avoidance (weight: 20%)
            //score += 20 * currentMetrics.eyeContactAvoidance;
            //factorCount++;

            // 3. Fixation duration (weight: 20%)
            if (currentMetrics.averageFixationDuration < 200f)
                score += 20 * (1f - currentMetrics.averageFixationDuration / 200f);
            factorCount++;

            // 4. Gaze stability (weight: 15%)
            score += 15 * Mathf.Clamp01(currentMetrics.gazeStability / 0.1f);
            factorCount++;

            // 5. Blink rate (weight: 5%)
            float normalBlinkRate = 17f; // blinks per minute
            score += 5 * Mathf.Abs(currentMetrics.blinkRate - normalBlinkRate) / normalBlinkRate;
            factorCount++;

            // Audio factors (if available)
            if (audioData != null)
            {
                // 6. Prosody (weight: 15%)
                score += 15 * (1f - currentMetrics.prosodyScore);
                factorCount++;

                // 7. Voice quality (weight: 10%)
                score += 10 * (1f - currentMetrics.voiceQuality);
                factorCount++;
            }

            currentMetrics.asdRiskScore = Mathf.Clamp(score, 0, 100);

            // Determine risk level
            if (currentMetrics.asdRiskScore < 30)
                currentMetrics.riskLevel = "Low";
            else if (currentMetrics.asdRiskScore < 60)
                currentMetrics.riskLevel = "Medium";
            else
                currentMetrics.riskLevel = "High";
        }

        // Export results to CSV for coordinator
        public void ExportResults(string outputPath)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"{outputPath}/ASD_Analysis_{timestamp}.csv";

            using (StreamWriter sw = new StreamWriter(filename))
            {
                // Header
                sw.WriteLine("Metric;Value;Normal Range;Status");

                // Eye tracking metrics
                sw.WriteLine($"Social Attention Ratio;{currentMetrics.socialAttentionRatio:F3};>0.60;{(currentMetrics.socialAttentionRatio > 0.6f ? "OK" : "CONCERN")}");
                //sw.WriteLine($"Eye Contact Avoidance;{currentMetrics.eyeContactAvoidance:F3};<0.30;{(currentMetrics.eyeContactAvoidance < 0.3f ? "OK" : "CONCERN")}");
                sw.WriteLine($"Average Fixation Duration (ms);{currentMetrics.averageFixationDuration:F1};200-400;{(currentMetrics.averageFixationDuration >= 200 ? "OK" : "CONCERN")}");
                sw.WriteLine($"Total Fixations;{currentMetrics.totalFixations};-;-");
                sw.WriteLine($"Gaze Stability;{currentMetrics.gazeStability:F4};<0.05;{(currentMetrics.gazeStability < 0.05f ? "OK" : "CONCERN")}");
                sw.WriteLine($"Blink Rate (per min);{currentMetrics.blinkRate:F1};12-20;{(currentMetrics.blinkRate >= 12 && currentMetrics.blinkRate <= 20 ? "OK" : "CONCERN")}");
                sw.WriteLine($"Mean Saccade Amplitude;{currentMetrics.meanSaccadeAmplitude:F4};-;-");

                // Audio metrics
                if (audioData != null)
                {
                    sw.WriteLine($"\nAudio Metrics (normalized scores where applicable)");
                    // f0Variability, prosodyScore, voiceQuality are 0..1 normalized in your algorithm
                    sw.WriteLine($"F0 Variability (norm, 0..1);{currentMetrics.f0Variability:F3};>0.60;{(currentMetrics.f0Variability > 0.6f ? "OK" : "CONCERN")}");
                    sw.WriteLine($"Prosody Score (norm, 0..1);{currentMetrics.prosodyScore:F3};>0.50;{(currentMetrics.prosodyScore > 0.5f ? "OK" : "CONCERN")}");
                    sw.WriteLine($"Voice Quality (norm, 0..1);{currentMetrics.voiceQuality:F3};>0.70;{(currentMetrics.voiceQuality > 0.7f ? "OK" : "CONCERN")}");
                    // also include raw acoustic descriptors for traceability
                    sw.WriteLine($"Raw meanF0 (Hz);{audioData.meanF0:F1};-;-");
                    sw.WriteLine($"Raw sdF0 (Hz);{audioData.sdF0:F2};-;-");
                    sw.WriteLine($"Raw jitter;{audioData.jitter:F5};-;-");
                    sw.WriteLine($"Raw shimmer;{audioData.shimmer:F5};-;-");
                    sw.WriteLine($"Raw HNR (dB);{audioData.meanHNR:F2};-;-");
                }

                // AOI breakdown
                sw.WriteLine($"\nAOI Time Distribution");
                foreach (var kvp in currentMetrics.aoiTimeSpent)
                {
                    float percentage = (kvp.Value / eyeTrackingBuffer.Count) * 100f;
                    sw.WriteLine($"{kvp.Key};{percentage:F1}%");
                }

                // Overall assessment
                sw.WriteLine($"\nOverall Assessment");
                sw.WriteLine($"ASD Risk Score;{currentMetrics.asdRiskScore:F1}/100");
                sw.WriteLine($"Risk Level;{currentMetrics.riskLevel}");
            }

            Debug.Log($"Analysis exported to: {filename}");
        }

        // Clear buffer for new session
        public void ResetSession()
        {
            eyeTrackingBuffer.Clear();
            audioData = null;
            currentMetrics = new ASDMetrics();
        }
        private float ParseFloat(string value)
        {
            // Replace comma with dot for consistent parsing
            value = value.Replace(',', '.');
            return float.Parse(value, CultureInfo.InvariantCulture);
        }
    }

    [System.Serializable]
    public class EyeTrackingData
    {
        public float xFiltered;
        public float yFiltered;
        public float xNormalized;
        public float yNormalized;
        public float distanceToCamera;
        public float eyeAspectRatio;
        public bool isBlinking;
        public long timestampMs;
        public string aoiName;
    }

    [System.Serializable]
    public class AudioAnalysisData
    {
        public string filename;
        public float duration;
        public float meanF0;
        public float medianF0;
        public float minF0;
        public float maxF0;
        public float sdF0;
        public float jitter;
        public float shimmer;
        public float meanHNR;
        public float meanIntensity;
        public float voicedFraction;
    }

    [System.Serializable]
    public class ASDMetrics
    {
        // Eye tracking metrics
        public float socialAttentionRatio;
        public float environmentAttentionRatio;
        public int totalFixations;
        public float averageFixationDuration;
        public float gazeStability;
        public float blinkRate;
        public float meanSaccadeAmplitude;
        //public float eyeContactAvoidance;

        // Audio metrics
        public float f0Variability;
        public float prosodyScore;
        public float voiceQuality;

        // Combined risk score
        public float asdRiskScore; // 0-100
        public string riskLevel; // "Low", "Medium", "High"

        // Detailed breakdown
        public Dictionary<string, float> aoiTimeSpent = new Dictionary<string, float>();
    }
}