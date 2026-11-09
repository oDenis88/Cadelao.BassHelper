@echo off
rem Compatibilidade: nome do comando usado na versao homologada 3.1.
rem A versao 3.2 so conclui uma publicacao quando o Chordino esta incluso.
call "%~dp0PUBLICAR_TUDO_INCLUIDO.cmd"
exit /b %ERRORLEVEL%
