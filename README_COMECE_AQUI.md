# Cadelao.BassHelper 3.2.0 — Chordino integrado, sem instaladores separados


> **Novidade 3.2 (versao offline):** se o Chordino 3.1 ja funciona neste computador, rode `INCORPORAR_CHORDINO_EXISTENTE.cmd` antes de abrir a solucao para incorporar os binarios existentes **sem nenhum novo download**. Para gerar um ZIP autossuficiente para outros PCs, execute `PUBLICAR_TUDO_INCLUIDO.cmd`. As instrucoes completas estao em `README_3_2_BUNDLE_E_CORES.md`. Cores agora sao distintas por cifra (ex.: Am rosa, Dm azul, E dourado); nenhuma regra musical foi alterada.

**Escopo:** WinForms Designer-friendly; MP3/WAV; reconhecedor real Chordino/NNLS Chroma; cifras com timestamps sincronizados; simplificacao das cifras para triades no baixo 4 cordas E-A-D-G; ajuste de sync e edicoes manuais persistentes.

## O que mudou em relacao a versao 3.0

- Botao `PREPARAR MOTOR`: verifica o Chordino, prepara o host/plugin automaticamente e informa o resultado. Nao abre dialogo pedindo para selecionar EXE ou instalar aplicativos.
- Na primeira analise de uma musica, se o motor ainda nao existir, pede uma unica autorizacao para baixar os componentes oficiais/open source no Windows x64. **Internet necessaria apenas nessa primeira preparacao**.
- O Chordino fica em `%LOCALAPPDATA%\Cadelao.BassHelper\Native\win-x64\`, no perfil do usuario. Nenhuma alteracao de registro, PATH global, privilegios de administrador ou instalador separado.
- Verifica a arquitetura PE x64 de host e DLL, verifica se o Vamp consegue listar realmente `nnls-chroma:chordino`, registra origens e SHA256. Quando o GitHub fornece o digest SHA-256 da release, confere esse valor.
- Reutiliza os componentes nas execucoes seguintes; se ja houver um Chordino funcional, nao baixa outra copia.
- **Modo offline:** com o motor preparado, execute `PUBLICAR_PORTATIL.cmd`. Ele compila uma pasta com .NET Runtime incluido e **copia** os binarios Chordino para `Tools` no aplicativo publicado. Depois a pasta pode ser copiada para outro Windows x64 sem instalar o runtime ou conectar na internet.

## Uso no Visual Studio

1. Extraia o ZIP inteiro para uma pasta.
2. Abra `Cadelao.BassHelper.sln` no Visual Studio com suporte a .NET 10 / Windows Forms.
3. Execute `Restore NuGet Packages`, `Rebuild` e F5.
4. Abra uma faixa MP3/WAV (ou `DEMO Dm/Am/E`). Na primeira utilizacao, aceite o download integrado de Chordino. Nao instale nada por fora.
5. Os acordes Chordino sao apresentados com timestamps brutos; acordes com setima sao reduzidos para triades no display, por exemplo `E7 -> E` e `Am7 -> Am`. A coluna bruta continua disponivel.
6. Use `SYNC(ms)` se desejar alinhar um pequeno atraso de buffer; edicoes feitas na tabela permanecem salvas em `%LOCALAPPDATA%\Cadelao.BassHelper\Edits\`.

## Publicacao sem .NET instalado no outro PC

Na pasta do projeto execute `PUBLICAR_PORTATIL.cmd`, em um Windows com o SDK .NET 10. Ele gera `dist\Cadelao.BassHelper-win-x64\` com a aplicacao self-contained. Para **offline completo**, execute primeiro o BassHelper uma vez com internet, configure o Chordino automaticamente e execute novamente o CMD para levar o host e a DLL juntos.

## Fontes de download, licencas e cuidado

1. Host Vamp SDK 2.10 x64 (fonte oficial): https://www.vamp-plugins.org/develop.html
2. Chordino Windows x64, release comunitaria do NNLS Chroma: https://github.com/jotapemj/nnls-chroma-win64/releases/tag/v1.1-win64
3. Plugin original e fonte GPL: https://github.com/c4dm/nnls-chroma

O **Chordino x64 e uma compilacao comunitaria**, nao um binario oficial de QMUL. O aplicativo pergunta antes de baixar. Os binarios nao estao fisicamente dentro deste ZIP porque nao foi possivel obte-los nesta sessao. Caso o GitHub esteja indisponivel, a preparacao falha com mensagem de diagnostico; o programa NAO substitui silenciosamente o Chordino por um detector proprio.

As gravacoes comerciais que voce utiliza permanecem na sua maquina; nao fazem parte do ZIP.

## Diagnosticos

`%LOCALAPPDATA%\Cadelao.BassHelper\Native\win-x64\bootstrap_status.json`

`%LOCALAPPDATA%\Cadelao.BassHelper\Native\win-x64\download_provenance.jsonl`

`%LOCALAPPDATA%\Cadelao.BassHelper\AnalysisCache\`

Se aparecer erro de rede, inspecione `bootstrap_status.json` e tente `PREPARAR MOTOR` quando a conexao estiver disponivel. Se o antivírus bloquear a DLL comunitaria, o proprio Windows pode impedir o carregamento e o teste de plugin explicara o problema.

## Validacao

O pacote teve codigo e organizacao revisados estaticamente, mas **nao foi compilado em Visual Studio nem executado com plugin Windows x64 neste ambiente**. Nao ha homologacao do processo de download/execucao ate o teste no seu Windows.
