@echo off
:menu
cls
echo ===================================================
echo    Froststrap Exporting Tool ( made cuz bored )
echo ===================================================
echo.
echo Type 'ex' to export
echo Type 'q' to quit
echo.

set /p input="Enter command: "

if /i "%input%"=="ex" goto export
if /i "%input%"=="q" exit /b

echo Invalid command. Please try again.
timeout /t 1 >nul
goto menu

:export
echo.
echo Starting publish process...
dotnet publish ./Bloxstrap/Bloxstrap.csproj --configuration Release /p:PublishProfile=Publish-x64 /p:PublishDir="C:\Users\meddsam\Downloads\Froststrap Stuff\Froststrap Export"

echo.
pause
goto menu