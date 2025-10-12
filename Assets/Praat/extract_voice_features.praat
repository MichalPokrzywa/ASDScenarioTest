# extract_voice_features.praat
# Usage (CLI): praat --run extract_voice_features.praat "input.wav" "out.csv"
# The script writes a single-line CSV (header + one line) with acoustic measures.

form Extract voice features
    sentence infile /path/to/input.wav
    sentence outfile /path/to/out.csv
endform

# --- Read sound
sound = Read from file: infile$
selectObject: sound

# --- Duration
duration_s = Get total duration

# --- Create Pitch and PointProcess (voice-analysis settings recommended for voice)
# The parameter set below is taken from Praat scripting recommendations for voice analysis.
pitch = To Pitch (cc): 0, 75, 15, "no", 0.03, 0.45, 0.01, 0.35, 0.14, 600

# --- Intensity and Harmonicity (HNR)
selectObject: sound
intensity = To Intensity: 75, 0.001
selectObject: sound
harm = To Harmonicity (cc): 0.01, 75, 0.1, 1

# --- Point process (for jitter/shimmer calculation)
# make sure Sound and Pitch are present, then create pulses
selectObject: sound, pitch
pp = To PointProcess (cc)

# --- Pitch statistics (mean, median, min, max, stdev)
selectObject: pitch
meanF0 = Get mean: 0, 0, "Hertz"
sdF0 = Get standard deviation: 0, 0, "Hertz"
medianF0 = Get quantile: 0, 0, 0.5, "Hertz"
minF0 = Get minimum: 0, 0, "Hertz", "Parabolic"
maxF0 = Get maximum: 0, 0, "Hertz", "Parabolic"

# --- Intensity mean (in dB)
selectObject: intensity
meanIntensity = Get mean: 0, 0

# --- HNR (mean)
selectObject: harm
meanHNR = Get mean: 0, 0

# --- Voice report: extract jitter & shimmer (local) from the voice report string
selectObject: sound, pitch, pp
voiceReport$ = Voice report: 0, 0, 75, 600, 1.3, 1.6, 0.03, 0.45
jitter_local = extractNumber (voiceReport$, "Jitter (local):")
shimmer_local = extractNumber (voiceReport$, "Shimmer (local):")
# optionally extract other fields if present:
# meanNoiseToHarmonics = extractNumber (voiceReport$, "Mean noise-to-harmonics ratio:")

# --- Voiced fraction (frames in Pitch object that have a positive F0)
selectObject: pitch
nframes = Get number of frames
voicedFrames = 0
for i to nframes
    time_i = Get time from frame number: i
    f0_i = Get value at time: time_i, "Hertz", "linear"
    if f0_i > 0
        voicedFrames = voicedFrames + 1
    endif
endfor
if nframes > 0
    voiced_fraction = voicedFrames / nframes
else
    voiced_fraction = 0
endif

# --- Write CSV (header + one data line). Overwrites existing file.
# Header:
writeFileLine: outfile$, "file;duration_s;meanF0;medianF0;minF0;maxF0;sdF0;jitter_local;shimmer_local;meanHNR;meanIntensity;voiced_fraction"
# Data line:
appendFileLine: outfile$, infile$, ";", duration_s, ";", meanF0, ";", medianF0, ";", minF0, ";", maxF0, ";", sdF0, ";", jitter_local, ";", shimmer_local, ";", meanHNR, ";", meanIntensity, ";", voiced_fraction

# --- Clean up created objects (optional)
selectObject: sound, pitch, intensity, harm, pp
#removeObject: sound, pitch, intensity, harm, pp

# Done
