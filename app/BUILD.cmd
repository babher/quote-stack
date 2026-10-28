@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "DESTDIR=%LOCALAPPDATA%\qsapp"
set "DEST=%DESTDIR%\QuoteStack.exe"
set "LOG=%~dp0build.log"
set "SRC=%CD%\QuoteStack.cs"
set "RC=0"

> "%LOG%" echo BUILD %DATE% %TIME%
>> "%LOG%" echo src=%SRC%
>> "%LOG%" echo dest=%DEST%

echo ============================================
echo  QuoteStack build
echo  log: %LOG%
echo ============================================

if not exist "%CSC%" goto nocsc
if not exist "dist" mkdir "dist"

echo [1/2] compiling ...
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /out:"dist\QuoteStack.exe" QuoteStack.cs > "dist\compile.out" 2>&1
set "CERR=%ERRORLEVEL%"
type "dist\compile.out"
>> "%LOG%" echo ---- compile output ----
>> "%LOG%" type "dist\compile.out"
>> "%LOG%" echo compile-exit=%CERR%
if not "%CERR%"=="0" goto buildfail
if not exist "dist\QuoteStack.exe" goto buildfail

echo [2/2] installing to %DESTDIR% ...
if not exist "%DESTDIR%" mkdir "%DESTDIR%"
echo stopping a running instance (a running exe cannot be replaced) ...
taskkill /IM QuoteStack.exe /F >nul 2>&1
ping -n 2 127.0.0.1 >nul
copy /y "dist\QuoteStack.exe" "%DEST%" > "dist\copy.out" 2>&1
set "CPERR=%ERRORLEVEL%"
if not "%CPERR%"=="0" goto copyretry
goto verify

:copyretry
>> "%LOG%" echo copy attempt 1 failed rc=%CPERR% - retry once
type "dist\copy.out"
ping -n 2 127.0.0.1 >nul
copy /y "dist\QuoteStack.exe" "%DEST%" > "dist\copy.out" 2>&1
set "CPERR=%ERRORLEVEL%"
if not "%CPERR%"=="0" goto copyfail
goto verify

:verify
type "dist\copy.out" >nul
for %%A in ("dist\QuoteStack.exe") do set "SSZ=%%~zA"
for %%A in ("%DEST%") do set "DSZ=%%~zA"
>> "%LOG%" echo ---- copy output ----
>> "%LOG%" type "dist\copy.out"
if not exist "%DEST%" goto copyfail
if not "%SSZ%"=="%DSZ%" goto sizemismatch
for %%A in ("%DEST%") do set "MT=%%~tA"
echo.
echo [OK] installed: %DEST%
echo      size: %DSZ% bytes (source %SSZ%)   mtime: %MT%
echo.
echo Next: run RUN.cmd , then press the hotkey in Edge.
>> "%LOG%" echo [OK] installed size=%DSZ% mtime=%MT%
set "RC=0"
goto done

:nocsc
echo [FAIL] csc.exe not found: %CSC%
>> "%LOG%" echo [FAIL] csc.exe not found
set "RC=3"
goto done

:buildfail
echo.
echo [FAIL] compile failed - see messages above
>> "%LOG%" echo [FAIL] compile failed
set "RC=1"
goto done

:copyfail
echo.
echo [FAIL] could not copy to %DESTDIR%
echo        do NOT run the exe from this folder: it runs at Low integrity
echo        and hotkey capture silently fails there.
>> "%LOG%" echo [FAIL] copy failed last-rc=%CPERR%
set "RC=2"
goto done

:sizemismatch
echo.
echo [FAIL] copied file size %DSZ% does not match source %SSZ%
>> "%LOG%" echo [FAIL] size mismatch dest=%DSZ% src=%SSZ%
set "RC=2"
goto done

:done
>> "%LOG%" echo RESULT rc=%RC%
echo.
echo ------------------ press any key ------------------
echo log written to %LOG%
pause >nul
exit /b %RC%