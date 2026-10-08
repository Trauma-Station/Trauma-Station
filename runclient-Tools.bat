@echo off
dotnet build --configuration Tools
dotnet run --project Content.Trauma.Client --configuration Tools
