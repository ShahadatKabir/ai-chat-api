@echo off
REM AI Chat API - Windows Docker Commands

if "%1"=="build" goto build
if "%1"=="run" goto run
if "%1"=="stop" goto stop
if "%1"=="logs" goto logs
if "%1"=="clean" goto clean
if "%1"=="export" goto export
goto help

:build
echo Building Docker image...
docker build -t aichatapi .
goto end

:run
echo Running Docker container...
docker run -d --name aichatapi-container -p 5000:5000 -e JWT_KEY=%JWT_KEY% -e GEMINI_API_KEY=%GEMINI_API_KEY% -e GEMINI_MODEL=%GEMINI_MODEL% aichatapi
echo Container started. Access at http://localhost:5000
goto end

:stop
echo Stopping Docker container...
docker stop aichatapi-container 2>nul
docker rm aichatapi-container 2>nul
echo Container stopped.
goto end

:logs
echo Showing Docker logs...
docker logs aichatapi-container
goto end

:clean
echo Cleaning up Docker images...
docker rmi aichatapi 2>nul
docker system prune -f
echo Cleanup complete.
goto end

:export
echo Exporting Docker image...
for /f "tokens=2 delims==" %%i in ('wmic os get localdatetime /value') do set datetime=%%i
set timestamp=%datetime:~0,8%_%datetime:~8,6%
docker save aichatapi > aichatapi_%timestamp%.tar
echo Image exported as aichatapi_%timestamp%.tar
goto end

:help
echo AI Chat API Docker Commands
echo.
echo Usage: docker-run.bat [command]
echo.
echo Commands:
echo   build    - Build the Docker image
echo   run      - Run the container (set JWT_KEY, GEMINI_API_KEY env vars first)
echo   stop     - Stop and remove the container
echo   logs     - Show container logs
echo   clean    - Remove image and clean up
echo   export   - Export image as tar file
echo.
echo Example:
echo   set JWT_KEY=your-jwt-key
echo   set GEMINI_API_KEY=your-gemini-key
echo   docker-run.bat build
echo   docker-run.bat run
goto end

:end