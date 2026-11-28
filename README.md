# Video Activity Review

Review recorded video for visible activity including sports, exercise, and
everyday physical tasks. The app creates timestamped draft notes and exports
them as a plain-text report.

## Run on Windows

For 64-bit Windows 10 or 11, follow [Build from source](#build-from-source)
to create the executable.

## Run on Mac

Download [VideoActivityReview-Mac.zip](./VideoActivityReview-Mac.zip) on macOS Sonoma 14 or newer.
Double-click the ZIP, then double-click **Video Activity Review**. You can drag the app to
**Applications** to keep it handy. On first launch the app downloads its runtime and opens
your browser. Existing installations reuse the downloaded model. Use **Open review** to reopen
the browser or **Quit** in the launcher after saving your work.

The app is unsigned. If macOS blocks the first launch, see [Apple's instructions for opening a trusted app](https://support.apple.com/guide/mac-help/mh40616/mac).
To build the Mac ZIP from source, run `python3 tools/build_mac.py VideoActivityReview-Mac.zip`.

## Analyze a video

1. Start the Windows executable or Mac launcher. The review interface opens
   in your browser. You can minimize the launcher window while it runs.
2. Choose a local recording. Use an H.264 MP4 if your browser can't play the
   original file.
3. Open **Claimant details**. Choose **Male claimant**, **Female claimant**,
   or **Unspecified**, and enter any clothing details you know.
4. Click **Analyze video**. If prompted, click **Download analysis engine**
   or **Update vision model**. When setup finishes, click **Analyze video**
   again.
5. To focus on someone, pause the video before analysis, choose **Select person in current frame**, and click their torso in the still image. If the appearance match becomes unclear, the app pauses. Seek to a clear frame, select again, and resume.
6. Click a timestamp to replay from two seconds before the note. Use **Confirm**, **Edit**, or **Dismiss**. Confirm an edited note to include it in **Export confirmed notes**.
7. **Stop** pauses analysis after the last completed segment. **Save review** downloads a local review file containing notes, decisions, settings, playback position, and the reference frame if selected. To resume after closing the app, choose the original recording and **Open saved review**. Save again after making changes.

## First run and model updates

First setup downloads about 5 GB on Windows or 3.5 GB on Mac and needs
additional disk space during extraction. The app saves these files in
`%LOCALAPPDATA%\VideoActivityReview` on Windows or
`~/Library/Application Support/VideoActivityReview` on Mac and reuses them.
It prompts again if the required model changes or the saved files are missing. Updating
from the original model downloads the replacement model and reuses the
existing engine.

After setup, analysis can run offline. The source footage stays on your
computer. No separate Node.js or Ollama installation is needed.

## Review the results

The app looks for lifting, carrying, pushing, pulling, walking, running,
jumping, climbing, bending, kneeling, squatting, reaching, exercise, sports,
cycling, swimming, manual work, and other visible physical activity.

Detailed review samples nine frames per four-second segment so brief movements are easier
to see. Choose **Faster overview** for eight-second segments and fewer model calls.
Both modes look for sitting down, standing up, straightening, turning, stepping,
shifting weight, and visible head, arm, hand, and leg movements. Each segment gets one short note about its clearest movement. The vocabulary includes grasping,
releasing, reaching overhead, using hand support, stepping up or down, crawling,
throwing, catching, and lowering objects. For lifts, pushes or pulls, catches,
and hand support, the model checks the visual evidence a second time. Possible lifts
are flagged for replay.
Timestamps are relative to the start of playback. Use **Show segments without
a clear action** to see quieter segments in the timeline. The text report contains only confirmed notes.
Consecutive draft notes with the same movement are grouped when the model sees a continuous action by the same person. Reviewed or edited notes keep their original ranges.
The app estimates an exposure adjustment for the analysis frames. You replay the original video.
If most samples have too little light, highlight detail, or contrast, the note reads
**Lighting obscures movement**. Visible frozen frames are marked unchanged.

The report records the claimant label you selected. With clothing cues, the
app can flag a possible match for that claimant. A selected frame can also guide appearance matching. Without clothing cues or a selection, notes describe general activity.

Example of a report entry with clothing cues and **Female claimant** selected.


```text
[00:00:08–00:00:12]
Possible clothing match for the female claimant.
Jogs along a paved path.
```

Similar outfits, obstructions, and camera cuts can make attribution uncertain. The app can
miss brief actions or mistake bending over an object for lifting it. Verify
the notes against the original footage before relying on them for an
insurance review.

## Reconnect to the launcher

If the review tab loses its connection, start the EXE again or select **Open
review window** in the running launcher. Select your video again in the new
tab. Save the review in the old tab and open that file in the new tab.
On Windows, select **Exit** in the launcher when you're finished. On Mac, click **Quit** in the launcher.

## Build from source

On Windows x64, extract the official Windows x64 ZIP for Node.js. Pass the
folder containing `node.exe` and its `LICENSE` to the build script.

```powershell
.\build-windows.ps1 -NodeDirectory C:\path\to\node
```

The script packages the launcher, local review interface, and Node runtime
into one Windows executable.
