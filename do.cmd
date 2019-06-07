@echo off
pushd "%~dp0"
call :main %*
popd
goto :EOF

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
goto :EOF

:build
    dotnet build -c Debug ^
 && dotnet build -c Release
goto :EOF

:test
    call :build ^
 && dotnet test --no-restore --no-build -c Debug   tests\Fixtures ^
 && dotnet test --no-restore --no-build -c Release tests\Fixtures
goto :EOF

:pack
setlocal
set VERSION_SUFFIX=
if not "%~1"=="" set VERSION_SUFFIX=--version-suffix %1
call :build ^
  && dotnet pack -c Release -o ..\dist ^
                 --no-restore          ^
                 --no-build            ^
                 --include-symbols     ^
                 --include-source      ^
                 %VERSION_SUFFIX%
goto :EOF
