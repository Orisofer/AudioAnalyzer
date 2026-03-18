# Ori.AudioAnalyzer

A robust audio analysis framework for Unity designed to bridge the gap between raw audio data and visualizable spectrograms. This tool provides a streamlined workflow for parsing signals and extracting frequency data for use in gameplay, UI, or music visualization.

## Project Structure

The system is built around a central **Orchestrator** that manages the state and execution of the analysis pipeline.

### Core Features
* **Decoupled Analysis:** Uses an `IAudioAnalyzer` interface to allow for flexible backend implementations.
* **Signal Parsing:** High-level abstraction for converting audio file paths into raw signal data.
* **Spectrogram Generation:** Efficiently transforms signals into frequency-over-time data.
* **State Persistence:** Caches the current signal and spectrogram for easy retrieval without re-processing.

---

## Technical Overview

### The Orchestrator
The `Orchestrator` is the internal engine of the `Ori.AudioAnalyzer.Core` namespace. It coordinates the lifecycle of an audio analysis session.

#### Key Methods

| Method | Description |
| :--- | :--- |
| `UpdateAudioPath(string path)` | Sets the target file path for the next parsing operation. |
| `ParseAudio(string path)` | Validates the path and invokes the analyzer to create a `Signal`. |
| `AnalyzeAudio(Signal signal)` | Generates a `Spectrogram`. If no signal is passed, it attempts to use the last parsed signal. |
| `Reset()` | Clears all cached data (Signal, Spectrogram, and Path) to start fresh. |

---

## Usage Example

Since the `Orchestrator` is currently marked as `internal`, it is intended to be used within the core assembly. Below is the conceptual workflow:

```csharp
// 1. Initialize the Orchestrator
var orchestrator = new Orchestrator();

// 2. Set the path and parse the file
orchestrator.UpdateAudioPath("Assets/Audio/MyTrack.wav");
Signal mySignal = orchestrator.ParseAudio("Assets/Audio/MyTrack.wav");

// 3. Generate the Spectrogram
Spectrogram mySpectrogram = orchestrator.AnalyzeAudio(mySignal);
```

## Requirements
Unity: 2021.3 LTS or newer.
