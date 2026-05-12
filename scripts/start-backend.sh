#!/usr/bin/env bash
set -e
cd "$(dirname "$0")/../backend/InstructionPlatform.Api"
dotnet restore
dotnet run --launch-profile https
