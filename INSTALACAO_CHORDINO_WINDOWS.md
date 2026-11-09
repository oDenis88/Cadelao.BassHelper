# Manual de contingencia (nao necessario no fluxo normal)

O Cadelao.BassHelper 3.1 faz o provisionamento automaticamente na primeira analise ou ao clicar PREPARAR MOTOR. Nao e necessario instalar aplicativos separados.

Caso o GitHub esteja inacessivel, leia os diagnosticos em `%LOCALAPPDATA%\Cadelao.BassHelper\Native\win-x64\bootstrap_status.json`.

Componentes usados:
- Host Vamp SDK 2.10 Windows x64: https://www.vamp-plugins.org/develop.html
- Chordino/NNLS Chroma Windows x64: https://github.com/jotapemj/nnls-chroma-win64/releases/tag/v1.1-win64

O app tambem aceita um bundle manualmente preparado: coloque o host e suas DLLs em `Tools\Host` ao lado do EXE, e `nnls-chroma.dll`/`.cat`/`.n3` em `Tools\Plugins`. O host e descoberto automaticamente.

**Atencao:** um Chordino compilado para x86 nao serve para o host x64.
