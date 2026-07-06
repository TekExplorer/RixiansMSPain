@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0bin\upload.ps1" -Channel %~1