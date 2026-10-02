@echo off
cd ../../

call dotnet run --project Content.Trauma.Client --no-build %*

pause
