@echo off
setlocal
set "KEY=%~1"
if "%KEY%"=="" set "KEY=Alt+Shift+Q"
set "DIR=%APPDATA%\quote-stack"
set "CFG=%DIR%\config.json"

echo ============================================
echo  QuoteStack - set hotkey
echo ============================================
if not exist "%DIR%" mkdir "%DIR%"
> "%CFG%" echo {"hotkey":"%KEY%"}

if not exist "%CFG%" (
  echo [FAIL] could not write %CFG%
  goto done
)

echo [OK] wrote %CFG%
echo.
type "%CFG%"
echo.
echo Now run RUN.cmd . In Edge, select text and press: %KEY%

:done
echo.
echo ------------------ press any key ------------------
pause >nul