@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
set "OUT=%ROOT%dist\Cadelao.BassHelper-win-x64"
set "BUNDLE=%ROOT%dist\Cadelao.BassHelper_3_3_0_WIN_X64_ALL_INCLUDED.zip"
echo.
echo ============================================================
echo  Cadelao.BassHelper 3.3 - BUNDLE TOTALMENTE OFFLINE
 echo  Publicacao self-contained + Chordino ja existente na maquina
 echo  NENHUM DOWNLOAD do Chordino sera feito.
echo ============================================================
echo.
call "%ROOT%INCORPORAR_CHORDINO_EXISTENTE.cmd"
if errorlevel 1 (
  echo.
  echo FALHA: motor nao esta completo. Publicacao interrompida.
  pause
  exit /b 1
)
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERRO: SDK .NET 10 nao encontrado; execute com o SDK do Visual Studio.
  pause
  exit /b 2
)
if exist "%OUT%" rmdir /s /q "%OUT%"
echo Publicando aplicativo com runtime .NET incluido...
dotnet publish "%ROOT%Cadelao.BassHelper\Cadelao.BassHelper.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "%OUT%"
if errorlevel 1 (
  echo ERRO: dotnet publish falhou.
  pause
  exit /b 3
)
if not exist "%OUT%\Cadelao.BassHelper.exe" (
  echo ERRO: executavel WinForms nao foi gerado.
  pause
  exit /b 4
)
if not exist "%OUT%\Tools\Host\vamp-simple-host.exe" (
  echo ERRO: host nativo ausente no resultado publicado.
  pause
  exit /b 5
)
if not exist "%OUT%\Tools\Plugins\nnls-chroma.dll" (
  echo ERRO: plugin nativo ausente no resultado publicado.
  pause
  exit /b 6
)
set "VAMP_PATH=%OUT%\Tools\Plugins"
"%OUT%\Tools\Host\vamp-simple-host.exe" --list-ids 2>&1 | findstr /I /C:"nnls-chroma:chordino" >nul
if errorlevel 1 (
  echo ERRO: o Chordino da pasta publicada nao foi carregado.
  pause
  exit /b 7
)
copy /Y "%ROOT%GPL-2.0.txt" "%OUT%\GPL-2.0.txt" >nul
copy /Y "%ROOT%TERCEIROS_E_LICENCAS.md" "%OUT%\TERCEIROS_E_LICENCAS.md" >nul
copy /Y "%ROOT%README_COMECE_AQUI.md" "%OUT%\README_COMECE_AQUI.md" >nul
copy /Y "%ROOT%README_3_2_BUNDLE_E_CORES.md" "%OUT%\README_3_2_BUNDLE_E_CORES.md" >nul
if not exist "%ROOT%dist" mkdir "%ROOT%dist"
if exist "%BUNDLE%" del /q "%BUNDLE%"
echo Compactando a pasta publicada para distribuir offline...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%OUT%\*' -DestinationPath '%BUNDLE%' -Force"
if errorlevel 1 (
  echo ERRO ao gerar o ZIP final. A pasta publicada continua disponivel.
  pause
  exit /b 8
)
if not exist "%BUNDLE%" (
  echo ERRO: ZIP de bundle nao foi gerado.
  pause
  exit /b 9
)
echo.
echo SUCESSO: pacote sem downloads no computador de destino.
echo Pasta: "%OUT%"
echo ZIP:   "%BUNDLE%"
echo Copie o ZIP para um Windows x64, extraia e execute Cadelao.BassHelper.exe.
echo.
pause
exit /b 0
