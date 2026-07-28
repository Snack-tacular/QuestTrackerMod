@echo off
setlocal enabledelayedexpansion
echo ================================================
echo   Building Quest Tracker Mod...
echo ================================================
cd /d "%~dp0"
dotnet build QuestTrackerMod.csproj -c Release
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Building succeeded! Deploying plugin...

    set "DEST_R2=C:\Users\luah8\AppData\Roaming\r2modmanPlus-local\SineusArenaSurvivors\profiles\Default\BepInEx\plugins\QuestTrackerMod"
    if not exist "!DEST_R2!" mkdir "!DEST_R2!"
    copy /Y "bin\Release\netstandard2.1\QuestTrackerMod.dll" "!DEST_R2!\QuestTrackerMod.dll"
    echo [OK] Deployed to r2modman profile: !DEST_R2!

    set "DEST_LOCAL=..\BepInEx\plugins\QuestTrackerMod"
    if exist "..\BepInEx\plugins" (
        if not exist "!DEST_LOCAL!" mkdir "!DEST_LOCAL!"
        copy /Y "bin\Release\netstandard2.1\QuestTrackerMod.dll" "!DEST_LOCAL!\QuestTrackerMod.dll"
        echo [OK] Deployed to local BepInEx: !DEST_LOCAL!
    )

    echo.
    echo ================================================
    echo   SUCCESS! Mod is ready. Launch via r2modman!
    echo ================================================
) else (
    echo.
    echo BUILD FAILED. Check errors above.
)
