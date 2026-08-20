@echo off
title TRELLIS 3D AI Generator
cd /d "X:\Github\TRELLIS"

echo ========================================================
echo   TRELLIS 3D AI Generator - GPU Accelerated
echo ========================================================
echo.
echo [1/2] Loading AI model into GPU... (Takes ~10-15s)
echo [2/2] Web browser will open automatically when ready!
echo.

".venv\Scripts\python.exe" app.py

pause
