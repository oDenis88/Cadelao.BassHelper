# Cadelao.BassHelper 3.1 — verificacoes e passos de aceite

- [x] Fonte da versao 3.0 preservada, com WinForms Designer `MainForm.Designer.cs` separado.
- [x] Novo gerenciador `PortableChordinoRuntime` faz download sob consentimento, sem instalador de sistema.
- [x] Origem host x64: release oficial Vamp SDK 2.10.
- [x] Origem Chordino x64: release/tag comunitaria v1.1-win64, via API GitHub.
- [x] Rejeita URLs nao HTTPS e hosts de download nao esperados.
- [x] Limite de download 120 MiB, arquitetura PE x64 e comparacao SHA256 quando o digest consta da release.
- [x] Verificacao real do plugin via `vamp-simple-host --list-ids` antes de aceitar sucesso.
- [x] Corretor/manual e a reducao de setimas para triades permanecem.
- [x] `PUBLICAR_PORTATIL.cmd` inclui .NET self-contained e copia host/plugin se ja preparados.
- [ ] Build real com .NET 10/Visual Studio no Windows (SDK indisponivel no ambiente atual).
- [ ] Download real dos dois releases (acesso binario GitHub bloqueado no ambiente atual).
- [ ] Verificacao dinamica em Windows do host e Chordino x64.
- [ ] Teste da precisao na gravacao comercial e alinhamento pelo ouvido.

**Sequencia sugerida:** Build -> DEMO -> consentir download -> verificar `READY` -> analisar MP3 -> corrigir se necessario -> PUBLICAR_PORTATIL -> copiar pasta offline para outro Windows x64 -> analisar novamente sem conexao.
