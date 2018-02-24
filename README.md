# Language Server client for MonoDevelop and Visual Studio for Mac

Provides Language Server support for MonoDevelop and Visual Studio for Mac.

API for integrating a language client is based on the [Visual Studio Language Server Protocol Client](https://docs.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension).

# Language Server Protocol Features

 - [x] initialize
 - [ ] initialized
 - [x] shutdown
 - [x] exit
 - [ ] $/cancelRequest
 - [x] window/showMessage
 - [x] window/showMessageRequest
 - [x] window/logMessage
 - [ ] telemetry/event
 - [ ] client/registerCapability
 - [ ] client/unregisterCapability
 - [x] workspace/didChangeConfiguration (Only sent on startup)
 - [ ] workspace/didChangeWatchedFiles
 - [x] workspace/symbol
 - [x] workspace/executeCommand
 - [x] workspace/applyEdit
 - [x] textDocument/publishDiagnostics
 - [x] textDocument/didOpen
 - [x] textDocument/didChange (Full and incremental)
 - [ ] textDocument/willSave
 - [ ] textDocument/willSaveWaitUntil
 - [ ] textDocument/didSave
 - [x] textDocument/didClose
 - [x] textDocument/completion
 - [x] completionItem/resolve
 - [x] textDocument/hover
 - [x] textDocument/signatureHelp
 - [x] textDocument/references
 - [ ] textDocument/documentHighlight
 - [ ] textDocument/documentSymbol
 - [x] textDocument/formatting
 - [x] textDocument/rangeFormatting
 - [ ] textDocument/onTypeFormatting
 - [x] textDocument/definition
 - [x] textDocument/codeAction
 - [ ] textDocument/codeLens
 - [ ] codeLens/resolve
 - [ ] textDocument/documentLink
 - [ ] documentLink/resolve
 - [x] textDocument/rename
 
 - [ ] Snippets

 - [x] Middleware
   - [x] ILanguageClientCompletionProvider
   - [x] ILanguageClientExecuteCommandProvider
   - [x] ILanguageClientWorkspaceSymbolProvider
 - [x] Connection
 - [x] IContentTypeMetadata
 - [x] CodeRemoteContentDefinition
 - [x] ILanguageClientCustomMessage
 - [ ] ILanguageClient
   - [x] ConfigurationSections
   - [ ] FilesToWatch
   - [x] InitializationOptions
   - [ ] StopAsync
   - [x] StartAsync
   - [x] ActivateAsync
   - [x] OnLoadedAsync

# Example Language Server Clients

 - Dockerfile
 - Java
 - Mock foo language
 - PowerShell
 - SQL
 - TypeScript
 - Yaml
 
