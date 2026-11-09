# QA — Cadelao.BassHelper 3.2.0 (2026-11-09)

**Escopo estrito:** paleta de 12 notas + builder offline a partir do cache nativo 3.1 existente.

- PASS: Baseline 3.1 preservada: Cadelao.BassHelper/MainForm.Designer.cs
- PASS: Baseline 3.1 preservada: Cadelao.BassHelper/Analysis/ChordinoTimeline.cs
- PASS: Baseline 3.1 preservada: Cadelao.BassHelper/Models/Chords.cs
- PASS: Baseline 3.1 preservada: Cadelao.BassHelper/Models/ChordEditStore.cs
- PASS: Baseline 3.1 preservada: Cadelao.BassHelper/Models/ChordExporter.cs
- PASS: Baseline 3.1 preservada: Cadelao.BassHelper/Audio/AudioPlayer.cs
- PASS: Baseline 3.1 preservada: Cadelao.BassHelper/Analysis/BassTrackAnalyzer.cs
- PASS: Baseline 3.1 preservada: Cadelao.BassHelper/Demo/demo_brega_triad_Dm_Am_E.wav
- PASS: Baseline 3.1 preservada: Cadelao.BassHelper.SignalTests/Program.cs
- PASS: Designer presente e nao vazio
- PASS: Assembly version 3.2.0
- PASS: Assets Tools copiados pelo MSBuild para output/publicacao
- PASS: 12 cores unicas (C ate B)
- PASS: Dm, Am, E com cores drasticamente diferentes
- PASS: Dm azul; Am rosa; E dourado
- PASS: Contraste texto/cores AA em todas as 12 paletas; minimo=4.74
- PASS: Timeline usa paleta por cifra
- PASS: Cifra atual e qualidade usam paleta
- PASS: Proxima cifra usa paleta
- PASS: Tabela e edicoes recoloridas
- PASS: Comportamento novo isolado na UI
- PASS: Host da pasta Tools preferido
- PASS: Chamada real Chordino preservada
- PASS: Script CMD completo: INCORPORAR_CHORDINO_EXISTENTE.cmd
- PASS: Script CMD completo: PUBLICAR_TUDO_INCLUIDO.cmd
- PASS: Script CMD completo: PUBLICAR_PORTATIL.cmd
- PASS: Bundle testa carregamento do plugin nativo
- PASS: Bundle falha sem a DLL, nao produz pacote falso
- PASS: Publicacao cria ZIP offline
- PASS: Sem edicoes no parser de acordes
- PASS: ZIP fonte nao finge conter os nativos; copia da maquina usuario

## Limites

Nao foi executado Rebuild WinForms (.NET SDK indisponivel aqui), nem execucao de EXE Windows nativo. As verificacoes sao estruturais, incluindo contraste de cores, comparacao binaria com a versao 3.1 e estrutura do instalador offline. O ZIP-fonte NAO inclui DLL/EXE Windows nativos; o comando no Windows os copia da instalacao existente e valida com Vamp --list-ids. Nao afirmar que esta pasta fonte e uma distribuicao pronta sem o passo de incorporar os binarios.
