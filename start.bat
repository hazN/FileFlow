@echo off
echo Starting FileFlow Development Environment...

:: Start the API
echo Launching API...
start "FileFlow API" cmd /k "cd FileFlow.API && dotnet run"

:: Start the React Client
echo Launching Client...
start "FileFlow Client" cmd /k "cd FileFlow.Client && npm run dev"

echo Both services are spinning up!
pause
