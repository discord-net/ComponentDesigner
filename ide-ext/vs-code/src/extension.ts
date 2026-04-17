import * as vscode from "vscode";
import * as languageClient from "vscode-languageclient";
import * as languageClientNode from "vscode-languageclient/node";
import * as path from "path";
import * as fs from "fs";

import jsonPreviewProvider from "./jsonPreviewProvider";

const languageServerPath: string =
  "server/ComponentDesigner.LanguageServer.exe";

let configuration: vscode.WorkspaceConfiguration =
  vscode.workspace.getConfiguration();

let outputChannel = vscode.window.createOutputChannel("Component Designer LSP");

async function activateLanguageServer(
  context: vscode.ExtensionContext
) {
  let pathFile: string = context.asAbsolutePath(languageServerPath);

  if (!fs.existsSync(pathFile)) {
    outputChannel.appendLine("Language server not found at path: " + pathFile);
    return;
  }

  let pathDir: string = path.dirname(pathFile);
  let serverOptions: languageClientNode.ServerOptions = {
    run: {
      command: pathFile,
      options: { cwd: pathDir },
    },
    debug: {
      command: pathFile,
      options: { cwd: pathDir },
    },
  };
  
  let clientOptions: languageClient.LanguageClientOptions = {
    documentSelector: ["cx"],
    synchronize: {
      configurationSection: "cx",
    },
  };

  let client = new languageClientNode.LanguageClient(
    "DiscordComponentsLanguageServer",
    "Discord Components Language Server",
    serverOptions,
    clientOptions,
  );

  let disposable = client.start();
  context.subscriptions.push(disposable);

  await client.onReady();

  context.subscriptions.push(client.onNotification("cx/preview-json", (params: {json: string, uri: string}) => {
    const uri = vscode.Uri.parse(params.uri);
    outputChannel.appendLine(`${uri.path}\n${params.json}`);
    jsonPreviewProvider.updateContent(uri, params.json);
  }));

  return client;
}

export async function activate(context: vscode.ExtensionContext) {
  outputChannel.appendLine("activiating...");

  await activateLanguageServer(context);

  context.subscriptions.push(vscode.workspace.registerTextDocumentContentProvider("cx-preview", jsonPreviewProvider));

  context.subscriptions.push(vscode.commands.registerCommand("cx.openPreview", async() => {
    if (!vscode.window.activeTextEditor) {
      return; // no editor
    }

    let { document } = vscode.window.activeTextEditor;

    if (document.languageId !== "cx") return;

    outputChannel.appendLine("Opening preview for: " + document.uri.toString());

    const previewUri = vscode.Uri.parse(`cx-preview:${document.uri.path}`);

    const doc = await vscode.workspace.openTextDocument(previewUri);
    vscode.languages.setTextDocumentLanguage(doc, "json");
    

    await vscode.window.showTextDocument(doc, {viewColumn: vscode.ViewColumn.Beside, preserveFocus: true});
  }));

  outputChannel.appendLine("CX extension has been activated");
}


export function deactivate() {}
