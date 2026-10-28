@echo off
setlocal
set "EXE=%LOCALAPPDATA%\qsapp\QuoteStack.exe"
if not exist "%EXE%" (
  echo [FAIL] not installed yet: %EXE%
  echo        run BUILD.cmd first.
  echo.
  echo ---------- press any key ----------
  pause >nul
  exit /b 1
)
echo stopping any running instance ...
taskkill /IM QuoteStack.exe /F >nul 2>&1
echo starting %EXE%
start "" "%EXE%"
echo.
echo The bar should appear at the bottom-right corner.
echo Hotkey comes from %APPDATA%\quote-stack\config.json