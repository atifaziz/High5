#!/usr/bin/env bash
[[ -e test.sh ]] || { echo >&2 "Please cd into the script location before running it."; exit 1; }
set -e
ARGS="$@"
if [ $# -eq 0 ]; then ARGS=Benchmarks; fi
dotnet run -c Release --project tests/Benchmarks/High5.Benchmarks.csproj -- $ARGS
