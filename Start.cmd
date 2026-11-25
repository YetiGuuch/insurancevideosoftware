@echo off
where node >nul 2>nul
if errorlevel 1 (
  echo Install Node.js 22 or newer, then reopen this launcher.
  pause
  exit /b 1
)
echo Open http://127.0.0.1:8765 in your browser.
echo Leave this window open while using Video Activity Review.
node "%~dp0server.mjs"
pause
