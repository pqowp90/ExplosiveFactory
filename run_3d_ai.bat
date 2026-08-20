@echo off
title TripoSR Fast 3D AI Generator
cd /d "X:\Github\TripoSR"

echo ========================================================
echo   TripoSR Fast 3D AI Generator (1-2s Generation)
echo   GPU: NVIDIA GeForce GTX 1660 SUPER (CUDA)
echo ========================================================
echo.
echo [1] Initializing model... (Takes ~3 seconds)
echo [2] Web browser will open automatically: http://127.0.0.1:7860
echo.

".venv\Scripts\python.exe" gradio_app.py

pause
