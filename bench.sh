#!/usr/bin/env bash
set -e
ARGS="$@"
if [ $# -eq 0 ]; then ARGS=Benchmarks; fi
dotnet run -c Release --project tests/Benchmarks/High5.Benchmarks.csproj -- $ARGS
