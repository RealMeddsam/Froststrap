@echo off
echo Exporting to EXE...
dotnet publish --configuration Release /p:PublishProfile=Publish-x64 --output "C:\Users\meddsam\Downloads\Froststrap Stuff\Froststrap Export"

if %ERRORLEVEL% EQU 0 (
    echo ✅ Export completed successfully!
) else (
    echo ❌ Export failed!
)

timeout /t 2 /nobreak >nul