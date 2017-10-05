@echo off
pushd "%~dp0"
dotnet run -c Release --project tests\Benchmarks\ParseFive.Benchmarks.csproj
popd
