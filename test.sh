#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
./build.sh
for c in Debug Release; do
    dotnet test --no-build -c $c tests/Fixtures
done
