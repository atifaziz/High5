@echo off
pushd "%~dp0"
dotnet test tests\Fixtures
popd
