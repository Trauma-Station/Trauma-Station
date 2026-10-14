#!/usr/bin/env bash
dotnet build --configuration Tools
dotnet run --project Content.Trauma.Server --configuration Tools
read -p "Press enter to continue"
