# Avisos de terceiros e redistribuicao

Este projeto e voltado a estudo musical privado e nao inclui as gravacoes comerciais do usuario.

- **Chordino / NNLS Chroma**, Matthias Mauch e outros, Queen Mary University of London. GPL v2 ou posterior. Fonte oficial https://github.com/c4dm/nnls-chroma . Compilacao comunitaria Windows x64: https://github.com/jotapemj/nnls-chroma-win64 . A redistribuicao da DLL acompanha os direitos/obrigacoes GPL, incluindo texto de licenca e disponibilidade de codigo-fonte correspondente. Uma copia do texto GPL v2 acompanha este pacote em `GPL-2.0.txt`.
- **Vamp plugin SDK / simple host**, Chris Cannam e colaboradores, permissivo estilo BSD. Fonte e notas: https://github.com/vamp-plugins/vamp-plugin-sdk ; versao 2.10 Windows x64 da release oficial. Preservar avisos de copyright/licenca ao redistribuir o host.
- **NAudio**, biblioteca .NET de audio, referenciada via NuGet no projeto. Licenca e fonte conforme o projeto upstream https://github.com/naudio/NAudio .

Os binarios nativos NAO estao embutidos no ZIP de codigo-fonte 3.1. Sao baixados pelo proprio aplicativo com autorizacao do usuario e guardados no perfil local, ou copiados para uma pasta portatil com `PUBLICAR_PORTATIL.cmd` depois de preparados. A consulta ao GitHub usa HTTPS e a verificacao de digest SHA-256 da release quando disponibilizada, mas nao substitui uma auditoria da compilacao comunitaria.
