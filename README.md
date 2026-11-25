# Video Activity Review

Review recorded video for visible activity including sports, exercise, and
everyday physical tasks. The app creates timestamped draft notes and exports
them as a plain-text report.

## Run on Windows

For 64-bit Windows 10 or 11, follow [Build from source](#build-from-source)
to create the executable.

## Analyze a video

1. Double-click the executable. It opens the review interface in your
   browser. You can minimize the launcher while it runs.
2. Choose a local recording. Use an H.264 MP4 if your browser can't play the
   original file.
3. Open **Claimant details**. Choose **Male claimant**, **Female claimant**,
   or **Unspecified**, and enter any clothing details you know.
4. Click **Analyze video**. If prompted, click **Download analysis engine**
   or **Update vision model**. When setup finishes, click **Analyze video**
   again.
5. Click a timestamp to replay that part of the video. Click **Download text
   report** to save the notes. **Stop** ends analysis early and keeps the
   completed segments available for export.

## First run and model updates

The first setup downloads about 5 GB of engine and model files and needs
additional disk space during extraction. The app saves these files in
`%LOCALAPPDATA%\VideoActivityReview` and reuses them on later runs. It prompts
again if the required model changes or the saved files are missing. Updating
from the original model downloads the replacement model and reuses the
existing engine.

After setup, analysis can run offline. The source footage stays on your
computer. No separate Node.js or Ollama installation is needed.

## Review the results

The app looks for lifting, carrying, pushing, pulling, walking, running,
jumping, climbing, bending, kneeling, squatting, reaching, exercise, sports,
cycling, swimming, manual work, and other visible physical activity.

Each segment covers up to eight seconds and uses nine sampled frames.
Timestamps are relative to the start of playback. Use **Show segments without
a clear action** to see quieter segments in the timeline. The text report
includes every completed segment.

The report records the claimant label you selected. With clothing cues, the
app can flag a possible match for that claimant. With the clothing fields left
blank, it describes general activity without assigning it to the claimant.

Example of a report entry with clothing cues and **Female claimant** selected.

`	ext
[00:00:08–00:00:16]
Possible clothing match for the female claimant.
Running — A person jogs along a paved path.
``n
Similar outfits, obstructions, and camera cuts can make attribution uncertain. The app can
miss brief actions or mistake bending over an object for lifting it. Verify
the notes against the original footage before relying on them for an
insurance review.

## Reconnect to the launcher

If the review tab loses its connection, start the EXE again or select **Open
review window** in the running launcher. Select your video again in the new
tab. If the old tab has a partial report, download it before closing the tab.
Use **Exit** in the launcher when you're finished.

## Build from source

On Windows x64, extract the official Windows x64 ZIP for Node.js. Pass the
folder containing `node.exe` and its `LICENSE` to the build script.

```powershell
.\build-windows.ps1 -NodeDirectory C:\path\to\node
```

The script packages the launcher, local review interface, and Node runtime
into one Windows executable.
