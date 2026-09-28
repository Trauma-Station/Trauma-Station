@echo off
dotnet build --configuration Tools
dotnet run --project Content.Trauma.Server --configuration Tools
pause
