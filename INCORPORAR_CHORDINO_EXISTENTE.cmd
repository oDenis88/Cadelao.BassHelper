@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
set "TARGET=%ROOT%Cadelao.BassHelper\Tools"
set "CACHED=%LOCALAPPDATA%\Cadelao.BassHelper\Native\win-x64"
set "HOST=%TARGET%\Host\vamp-simple-host.exe"
set "PLUGIN=%TARGET%\Plugins\nnls-chroma.dll"
echo.
echo ============================================================
echo  Cadelao.BassHelper 3.2 - INCORPORAR MOTOR OFFLINE
 echo  Sem rede, sem instaladores, sem mudar PATH do Windows.
echo ============================================================
echo.
if not exist "%HOST%" (
  if not exist "%CACHED%\Host\vamp-simple-host.exe" (
    echo ERRO: Host Vamp nao encontrado no cache do BassHelper 3.1.
    echo Esperado: "%CACHED%\Host\vamp-simple-host.exe"
    echo Este pacote nao baixa nada. E necessario ter usado o 3.1
    echo com o motor funcionando ao menos uma vez neste Windows.
    exit /b 2
  )
  if not exist "%TARGET%\Host" mkdir "%TARGET%\Host"
  xcopy "%CACHED%\Host\*" "%TARGET%\Host\" /E /I /Y /Q >nul
  if errorlevel 1 (
    echo ERRO ao incorporar Vamp Host.
    exit /b 3
  )
)
if not exist "%PLUGIN%" (
  if not exist "%CACHED%\Plugins\nnls-chroma.dll" (
    echo ERRO: Chordino DLL nao encontrada no cache do BassHelper 3.1.
    echo Esperado: "%CACHED%\Plugins\nnls-chroma.dll"
    exit /b 4
  )
  if not exist "%TARGET%\Plugins" mkdir "%TARGET%\Plugins"
  xcopy "%CACHED%\Plugins\*" "%TARGET%\Plugins\" /E /I /Y /Q >nul
  if errorlevel 1 (
    echo ERRO ao incorporar Chordino.
    exit /b 5
  )
)
if not exist "%HOST%" (
  echo ERRO: Host nao encontrado apos copia.
  exit /b 6
)
if not exist "%PLUGIN%" (
  echo ERRO: Chordino nao encontrado apos copia.
  exit /b 7
)
set "VAMP_PATH=%TARGET%\Plugins"
echo Verificando se o host realmente lista nnls-chroma:chordino...
"%HOST%" --list-ids 2>&1 | findstr /I /C:"nnls-chroma:chordino" >nul
if errorlevel 1 (
  echo ERRO: Host nao carregou o plugin embutido.
  echo Revise as DLLs nativas e tente novamente. Nao vou publicar bundle incompleto.
  exit /b 8
)
echo.
echo OK: motor incluido no projeto, sem nova transferencia.
echo Host:   "%HOST%"
echo Plugin: "%PLUGIN%"
echo No Visual Studio, Rebuild copiara Tools para bin\... automaticamente.
echo.
exit /b 0
