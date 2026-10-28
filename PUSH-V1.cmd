@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ==== Quote Stack v1.15  uploading to GitHub ====
echo.

git remote get-url origin 1>nul 2>nul
if errorlevel 1 (
  git remote add origin https://github.com/babher/quote-stack.git
  echo [1/4] remote added: https://github.com/babher/quote-stack.git
) else (
  echo [1/4] remote already configured
)

echo.
echo [2/4] asking GitHub for the default branch name ...
set "RB=main"
for /f "tokens=2" %%b in (^'git ls-remote --symref origin HEAD 2^>nul ^| findstr /b "ref:"^') do set "REFR=%%b"
if defined REFR set "RB=!REFR:refs/heads/=!"
echo       default branch = !RB!
if /i not "!RB!"=="main" (
  git branch -M "!RB!"
  echo       local branch renamed to !RB!
)

git fetch origin 1>nul 2>nul
git rev-parse --verify --quiet "origin/!RB!" 1>nul 2>nul
if errorlevel 1 (
  echo [3/4] remote branch has no commits yet - nothing to merge
) else (
  echo [3/4] remote already has commits - putting ours on top of them
  git rebase "origin/!RB!"
  if errorlevel 1 goto fail
)

echo.
echo [4/4] pushing branch !RB! ...
git push -u origin "!RB!"
if errorlevel 1 goto fail

echo.
echo        pushing tags v1 .. v1.15 ...
git push origin --tags
if errorlevel 1 (
  echo        a tag already exists on GitHub at an older commit - overwriting tags only
  git push -f origin --tags
  if errorlevel 1 goto fail
)

echo.
echo [OK] done.  https://github.com/babher/quote-stack
echo      Expected: 12 files, and tags v1 .. v1.15.
pause
exit /b 0

:fail
echo.
echo [FAIL] see the lines above. Common causes:
echo    1. repo name is not "quote-stack" - edit the git remote add line in this file.
echo    2. not signed in - run again and finish the browser login.
echo    3. a rebase conflict - run: git rebase --abort  then tell the developer.
pause
exit /b 1
