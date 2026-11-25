# Video Activity Review

Choose a recording, analyze it, and download a plain-text report. The local
vision model looks for visible lifting, carrying, walking, bending, and other
human activity. Each timestamp opens the corresponding segment in the video.

## First-time setup on Windows

1. Install [Node.js 22 or newer](https://nodejs.org/en/download) and
   [Ollama for Windows](https://ollama.com/download/windows).
2. Open a new PowerShell window and run `ollama pull qwen3-vl:4b`.
   The model download is approximately 3.3 GB.
3. Keep the app files together. Double-click `Start.cmd` and open
   [Video Activity Review](http://127.0.0.1:8765) in Edge or Chrome.
4. Choose a short H.264 MP4, select **Analyze video**, then
   **Download text report**. Keep the page open during analysis.

The model runs on your computer. The app reads the source video in the browser
and sends sampled images to Ollama on the same computer. The source file stays
in place. The browser keeps notes in memory until you download them or close
the page. To end a run early, select **Stop** and download the completed
segments.

## How to read the report

Each row covers up to eight seconds sampled at nine evenly spaced points.
Times are elapsed playback times. Brief actions, small objects, obstructions,
and camera changes can cause errors. Use the timestamp buttons to verify the
draft observations against the footage. Start with a short clip containing
known actions to check speed and accuracy on your computer.
