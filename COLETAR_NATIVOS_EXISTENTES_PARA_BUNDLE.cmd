@echo off
setlocal EnableExtensions
set "BASE=%LOCALAPPDATA%\Cadelao.BassHelper\Native\win-x64"
set "HOST=%BASE%\Host\vamp-simple-host.exe"
set "PLUGIN=%BASE%\Plugins\nnls-chroma.dll"
set "OUTPUT=%~dp0Cadelao.BassHelper_NATIVOS_WIN_X64_EXISTENTES.zip"
echo.
echo Cadelao.BassHelper - preparar nativos para bundle, SEM REDE.
echo.
if not exist "%HOST%" (
  echo ERRO: Vamp Host nao encontrado: "%HOST%"
  exit /b 2
)
if not exist "%PLUGIN%" (
  echo ERRO: Chordino x64 nao encontrado: "%PLUGIN%"
  exit /b 3
)
set "VAMP_PATH=%BASE%\Plugins"
"%HOST%" --list-ids 2>&1 | findstr /I /C:"nnls-chroma:chordino" >nul
if errorlevel 1 (
  echo ERRO: host nao conseguiu carregar o plugin x64.
  exit /b 4
)
if exist "%OUTPUT%" del /q "%OUTPUT%"
powershell -NoProfile -Command "Compress-Archive -Path '%BASE%\Host','%BASE%\Plugins' -DestinationPath '%OUTPUT%' -Force"
if errorlevel 1 exit /b 5
if not exist "%OUTPUT%" exit /b 6
echo.
echo OK: arquivo criado localmente, sem downloads:
echo "%OUTPUT%"
echo Envie este ZIP para compor a versao realmente all-included.
exit /b 0
