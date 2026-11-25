# Video Activity Review

Turn recorded video into timestamped draft notes about visible activity such
as lifting, carrying, walking, and bending. The app analyzes sampled frames
locally and exports a plain-text report.

## Run on 64-bit Windows 10 or 11

1. Double-click `VideoActivityReview.exe`. The review interface opens in your
   browser. Keep the launcher window open.
2. Choose a local recording. An H.264 MP4 is a good starting point.
3. Optionally describe the person's top or jacket, pants or shorts, shoes,
   hat, or an accessory. Leave unknown details blank.
4. Select **Analyze video**. On the first run, select **Download analysis
   engine** when prompted. The app downloads about 5 GB of engine and model
   files. This requires Internet access and several additional GB for
   extraction. Once setup finishes, select **Analyze video** again.
5. Select a timestamp to review the source segment, then choose
   **Download text report** to save your notes.

The executable includes the app and Node runtime. It uses your existing
browser and downloads the portable Ollama engine and model into
`%LOCALAPPDATA%\VideoActivityReview`. There are no separate installers or
accounts. Subsequent analyses run locally, including when offline. The source
video stays on your computer.

## Read the results

Clothing cues help the model focus on a plausible match within each segment.
Similar outfits, changes in clothing, obstacles, or poor image quality can
make attribution uncertain. Review segments marked as uncertain and verify
possible matches against the source. With no cues, the app describes general
human activity. The text export lists all completed segments, including those
with no visible match or action. Select **Stop** to keep a partial report.

Each segment covers up to eight seconds and uses nine sampled frames. Brief
actions can be missed. Times are elapsed playback times. Processing speed
depends on your hardware and footage.

## Build from source

On Windows x64, download the official Windows x64 ZIP for Node.js and extract
it. Pass the folder containing `node.exe` and its `LICENSE` to the build script.

```powershell
.\build-windows.ps1 -NodeDirectory C:\path\to\node
```

The script uses the .NET Framework compiler included with Windows to embed
the Windows launcher, local review app, and Node runtime into a single EXE.
