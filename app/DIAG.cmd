@echo off
setlocal
set "OUT=%~dp0diag-out.txt"
set "EXE=%LOCALAPPDATA%\qsapp\QuoteStack.exe"
set "CFG=%APPDATA%\quote-stack\config.json"
set "LOG=%APPDATA%\quote-stack\startup.log"

echo == when == > "%OUT%"
date /t >> "%OUT%"
time /t >> "%OUT%"

echo. >> "%OUT%"
echo == installed exe == >> "%OUT%"
if exist "%EXE%" (echo FOUND %EXE% >> "%OUT%" & dir "%EXE%" >> "%OUT%") else (echo MISSING %EXE% >> "%OUT%")

echo. >> "%OUT%"
echo == config == >> "%OUT%"
if exist "%CFG%" (type "%CFG%" >> "%OUT%") else (echo MISSING %CFG% >> "%OUT%")

echo. >> "%OUT%"
echo == startup.log == >> "%OUT%"
if exist "%LOG%" (type "%LOG%" >> "%OUT%") else (echo MISSING %LOG% >> "%OUT%")

echo. >> "%OUT%"
echo == processes before launch == >> "%OUT%"
tasklist /v /fi "imagename eq QuoteStack.exe" >> "%OUT%" 2>&1

echo. >> "%OUT%"
echo == launching == >> "%OUT%"
if exist "%EXE%" start "" "%EXE%"
timeout /t 3 /nobreak >nul

echo. >> "%OUT%"
echo == processes after 3s == >> "%OUT%"
tasklist /v /fi "imagename eq QuoteStack.exe" >> "%OUT%" 2>&1

echo. >> "%OUT%"
echo == startup.log after launch == >> "%OUT%"
if exist "%LOG%" (type "%LOG%" >> "%OUT%") else (echo MISSING %LOG% >> "%OUT%")

echo done - tell the agent
timeout /t 2 /nobreak >nul