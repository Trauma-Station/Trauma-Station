#!/usr/bin/env bash
dotnet build --configuration Tools
dotnet run --project Content.Trauma.Client --configuration Tools
read -p "Press enter to continue"
