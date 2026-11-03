#!/usr/bin/env bash
dotnet build
dotnet run --project Content.Trauma.Client
read -p "Press enter to continue"
