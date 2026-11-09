# Fontes open source avaliadas

| Projeto | O que faz | Integracao recomendada |
| --- | --- | --- |
| **Chordino/NNLS Chroma** | Vamp/C++; acordes com timestamps de entrada, HMM/Viterbi e ajuste de afinação | **Integrado ao BassHelper v3.0 via VampSimpleHost externo** |
| **QM Vamp Plugins** | Batidas, compassos, tonalidade, mudanças tonais | Beat tracker opcional integrado; não sobrepõe os timestamps originais |
| **ChordMiniApp** | Chord CNN-LSTM/BTC e modelos de batidas, interface web | Alternativa neural open source; backend Python mais pesado, não incluído no WinForms |
| **ChordFlask** | Experiência local parecida com Chordify; Chordino + QM, edição por batida | Referência arquitetural para UX, correções e cache |
| **Sonic Visualiser** | App científico para visualizar áudio, executar Vamp e exportar anotações | Ferramenta de auditoria da instalação do Chordino |

O software do Chordify não é open source. "Funcionar como Chordify" significa reproduzir o fluxo de reconhecimento de acordes + marcação de batidas + reprodução sincronizada, não copiar seu algoritmo proprietário.

Fontes:
- https://chordify.net/pages/technology-algorithm-explained/
- https://www.vamp-plugins.org/download.html
- https://github.com/c4dm/nnls-chroma
- https://github.com/ptnghia-j/ChordMiniApp
- https://github.com/bkl2000/chordflask
- https://www.sonicvisualiser.org/
