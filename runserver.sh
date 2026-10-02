#!/usr/bin/env bash
dotnet build
dotnet run --project Content.Trauma.Server
read -p "Press enter to continue"
