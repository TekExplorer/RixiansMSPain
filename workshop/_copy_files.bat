@echo off

echo About to copy mod files from the mods folder!
pause

:: Define source and destination paths
set "SRC=D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\HideDetailsMod"
set "DEST=D:\ModUploader-win-x64\HideDetailsMod\content"

:: Copy contents, overwrite matching files, and create destination if missing
xcopy "%SRC%\*" "%DEST%\" /E /I /Y

echo Mod files copied successfully!
pause