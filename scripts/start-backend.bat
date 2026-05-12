@echo off
cd /d %~dp0..\backend\InstructionPlatform.Api
dotnet restore
dotnet run --launch-profile https
