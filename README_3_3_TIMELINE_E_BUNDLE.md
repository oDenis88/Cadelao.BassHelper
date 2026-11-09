# Cadelao.BassHelper 3.3 - Timeline com zoom + estado real do bundle

## 1. Timeline ampliada, audio intacto

- A barra de progresso original da musica continua representando **a faixa inteira**.
- A timeline de acordes utiliza **uma janela movel de 30 segundos por padrao**.
- `JANELA (s)`: escolha 15 a 180 s. Quanto menor, mais espaco horizontal cada acorde ocupa. A duracao do arquivo MP3/WAV, a velocidade, os timestamps recebidos do Chordino e os metadados nao sao modificados.
- A barra horizontal abaixo dos acordes permite examinar outra parte da musica.
- `SEGUIR: ON`: o marcador acompanha a reproducao automaticamente. Ao rolar manualmente, vira `SEGUIR: OFF`; clique para reativar ou aperte PLAY.
- Clicar na timeline ou dar duplo clique na tabela continua fazendo seek para o tempo absoluto correto.
- O programa desenha apenas segmentos e batidas dentro da janela atual, preservando os eventos curtos que vieram do Chordino.
- **Cores da versao 3.2 preservadas byte a byte**: Dm azul, Am rosa, E dourado e as demais 9 fundamentais.
- O Windows Forms Designer permanece editavel (MainForm.Designer.cs e PhraseTimelineControl.cs).

### Exemplo de tamanho real

Para uma musica com 180 s, antes os 180 s ocupavam a largura total. Agora **30 s** ocupam a largura total. Uma frase de 5 s fica aproximadamente **6 vezes mais larga**, sem alterar o tempo de nenhuma nota.

## 2. Sobre a exigencia de ZIP literalmente all included

**Este projeto fonte NAO contem os executaveis Windows nativos do Chordino/Vamp.** A versao 3.2 tambem nao os continha: seu `PUBLICAR_TUDO_INCLUIDO.cmd` cria um bundle com os arquivos reais encontrados no seu computador.

Nesta sessao os arquivos `nnls-chroma.dll` e `vamp-simple-host.exe` nao estavam disponiveis e as tentativas de transferencia das releases oficiais falharam. Nao inserimos DLL ficticia nem afirmamos ter entregue um bundle funcional sem elas.

Voce ja possui esses dois componentes porque o 3.1 foi homologado em sua maquina. Para que possamos produzir um **ZIP entregue pronto, com os binarios originais incorporados e sem nenhum download**, envie o ZIP criado por `COLETAR_NATIVOS_EXISTENTES_PARA_BUNDLE.cmd`, diretamente desta solucao, ou compacte manualmente esta pasta:

`%LOCALAPPDATA%\Cadelao.BassHelper\Native\win-x64\`

Ela deve conter:
- `Host\vamp-simple-host.exe` (e as dependencias dele, se houver)
- `Plugins\nnls-chroma.dll` (e .cat/.n3 opcionais)

**Nao inclua MP3 nem arquivos pessoais** nesse envio.

### Opcao ja existente, sem downloads de componentes nativos

Em sua maquina, pode executar `PUBLICAR_TUDO_INCLUIDO.cmd` para montar um bundle executavel Windows x64 (requer o SDK .NET e a restauracao NuGet previamente disponiveis). O comando usa os binarios **locais** do Chordino e inclui o .NET runtime. Nao baixa o motor. Isso gera `dist\Cadelao.BassHelper_3_3_0_WIN_X64_ALL_INCLUDED.zip`.

## 3. Instalacao do patch de timeline

Aplique o arquivo ZIP do **patch** na raiz da solucao 3.2, onde esta `Cadelao.BassHelper.sln`, preservando a pasta `Cadelao.BassHelper\Tools` que ja estiver presente.

Abra o Visual Studio, Rebuild e F5. Os controles ficam na aba principal, acima dos acordes. O plugin Chordino do 3.2 permanece inalterado.

## 4. Limites dos testes

- Verificacao estatica dos fontes C# e do pacote ZIP.
- Foram testadas matematicamente a escala temporal, os offsets e a preservacao dos timestamps do Chordino.
- Nao ha SDK .NET/WinForms neste ambiente, logo nao houve compilacao Windows nem teste grafico no sistema de destino.
