@echo off

REM set path
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe"
set PROJECT_PATH="..\..\Cook Project"
set LOG_PATH=BatchModeBuildLog.txt
REM output path is relative to project path
set OUTPUT_PATH="../docs"

set args=-outputpath %OUTPUT_PATH% -webgl -fastbuild

echo ===========================================
echo   Unity Batch Build Started
echo ===========================================
echo Project: %PROJECT_PATH%
echo LogFile: %LOG_PATH%
echo Output Path: %OUTPUT_PATH%
echo.
echo Launching Unity in batch mode and build, this will take some time...
%UNITY_PATH% -batchmode -nographics -quit -projectPath %PROJECT_PATH% -executeMethod BuildPipeline.Build -logFile %LOG_PATH% %args%
echo.
echo ===========================================
if %ERRORLEVEL% EQU 0 (
    echo Build completed successfully!
) else (
    echo Build failed! Exit code: %ERRORLEVEL%
)
echo Log written to: %LOG_PATH%
echo ===========================================

pause