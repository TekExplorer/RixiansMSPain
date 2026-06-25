pause

if not exist ".\uploader\ModUploader.exe" (
	powershell -NoProfile -ExecutionPolicy Bypass -File ".\_get_uploader.ps1"
	if errorlevel 1 goto :eof
)

".\uploader\ModUploader.exe" upload -w .

pause