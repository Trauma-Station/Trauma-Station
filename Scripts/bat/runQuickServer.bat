@echo off
cd ../../

call dotnet run --project Content.Trauma.Server --no-build %*

pause
