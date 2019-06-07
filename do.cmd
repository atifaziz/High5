@echo off
pushd "%~dp0"
call :main %*
popd
exit /b %ERRORLEVEL%

:main
setlocal
where dotnet >nul 2>&1 || (
    echo>&2 .NET Core does not appear to be installed on this machine, which is
    echo>&2 required to build the solution. You can install it from the URL below
    echo>&2 and then try building again:
    echo>&2 https://dot.net
    exit 1
)
call :%1 %2 %3 %4 %5 %6 %7 %8 %9
exit /b %ERRORLEVEL%

:build
    dotnet build -c Debug ^
 && dotnet build -c Release
exit /b %ERRORLEVEL%

:test
    call :build ^
 && dotnet test --no-restore --no-build -c Debug   tests\Fixtures ^
 && dotnet test --no-restore --no-build -c Release tests\Fixtures
exit /b %ERRORLEVEL%

:pack
setlocal
set VERSION_SUFFIX=
if not "%~1"=="" set VERSION_SUFFIX=--version-suffix %1
call :build ^
  && dotnet pack -c Release            ^
                 --no-restore          ^
                 --no-build            ^
                 %VERSION_SUFFIX%
exit /b %ERRORLEVEL%
