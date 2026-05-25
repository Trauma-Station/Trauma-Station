@echo off
dotnet run --project Content.Trauma.Server
timeout /t 20 /nobreak
dotnet run --project Content.Trauma.Client