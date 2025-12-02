@echo off
REM Sample SPA Startup Script for Windows
REM This script will try to start the server using available tools

echo ============================================================
echo Sample SPA - OAuth 2.0 Authorization Code Flow
echo ============================================================
echo.

REM Check for Python
where python >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Starting server with Python...
    echo.
    python serve.py
    goto :end
)

REM Check for Node.js
where node >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Starting server with Node.js...
    echo.
    node serve.js
    goto :end
)

REM No suitable runtime found
echo ERROR: Neither Python nor Node.js found!
echo.
echo Please install one of the following:
echo   - Python 3: https://www.python.org/downloads/
echo   - Node.js: https://nodejs.org/
echo.
echo After installation, run this script again.
pause
goto :end

:end

