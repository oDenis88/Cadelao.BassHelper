# Cadelao.BassHelper 3.2.0 — Palette por cifra e bundle offline

## O que mudou (somente duas coisas)

1. **Cores por fundamental**: 12 cores contrastantes para C, C#, D, D#, E, F, F#, G, G#, A, A# e B. O `m` continua indicando acorde menor; as cores sao baseadas na fundamental, nao na qualidade maior/menor. Exemplo de sua musica: **Dm = azul vivo; Am = rosa forte; E = amarelo-dourado**. Linha do tempo, cifra grande, cifra seguinte e celula da tabela apresentam a mesma cor. As cifras continuam em texto e o contraste de cada celula e calculado automaticamente.
2. **Publicacao 100% offline apos incorporar o motor existente**: o computador em que o 3.1 ja funciona tem os binarios em `%LOCALAPPDATA%\Cadelao.BassHelper\Native\win-x64\`. O comando `INCORPORAR_CHORDINO_EXISTENTE.cmd` os coloca em `Cadelao.BassHelper\Tools`, e o projeto copia automaticamente essa pasta para o output do Visual Studio. `PUBLICAR_TUDO_INCLUIDO.cmd` publica o programa x64 *self-contained*, confere se o plugin aparece na lista do Vamp e gera um ZIP final com tudo incluido. O computador de destino nao baixa Chordino, nao instala Vamp nem instala o runtime .NET.

## Como usar (nesta mesma maquina onde o 3.1 ja funciona)

- Extraia o projeto 3.2, rode **INCORPORAR_CHORDINO_EXISTENTE.cmd** uma vez e abra `Cadelao.BassHelper.sln`.
- No Visual Studio: Rebuild -> F5. As cifras terao as novas cores, Chordino continua igual.
- Para um pacote independente: execute **PUBLICAR_TUDO_INCLUIDO.cmd**. Ele gera:
  - `dist\Cadelao.BassHelper-win-x64\Cadelao.BassHelper.exe`
  - `dist\Cadelao.BassHelper_3_2_0_WIN_X64_ALL_INCLUDED.zip`
- No computador de destino basta extrair o ZIP e executar o EXE, inclusive sem internet.

## Se o script nao encontrar Chordino

Ele avisa quais arquivos estao faltando e **interrompe** a publicacao em vez de produzir uma versao enganosa. Este ZIP-fonte **nao contem os executaveis nativos**: nao foi possivel obter os binarios Windows no ambiente em que preparei o patch. O script recupera a copia que **voce ja instalou e validou** com o 3.1, sem efetuar downloads. Ao criar o ZIP via `PUBLICAR_TUDO_INCLUIDO.cmd`, esse ZIP gerado no seu computador **contem** o motor e o runtime .NET.

## O que NAO mudou

Chordino/NNLS Chroma, NAudio, parser das cifras, detector de batidas opcional, timestamps, sincronizacao, edicoes manuais, exportacao, projeto WinForms Designer, estrutura da tela, notas do baixo de 4 cordas, MP3/WAV, parametros de analise e WAV demonstrativo. `MainForm.Designer.cs` nao foi modificado.

## Licencas

Chordino/NNLS Chroma e GPL-2.0-or-later (C4DM/QMUL). O Vamp SDK/host usa licencas BSD/X11. A publicacao inclui `GPL-2.0.txt` e `TERCEIROS_E_LICENCAS.md`, mas caso distribua binarios do Chordino publicamente, respeite as obrigacoes de disponibilizacao do codigo-fonte correspondente. Para uso pessoal, o bundler apenas consolida arquivos que voce ja tem.

## Validacao

Verificacao estatica de edicoes e estrutura da solucao, sem executar no Windows. Os scripts Windows fazem uma validacao concreta com `--list-ids` antes de concluir o bundle. O Rebuild e a validacao funcional no seu Visual Studio ainda sao necessarios.
