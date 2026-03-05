import * as vscode from "vscode";
import * as languageClient from "vscode-languageclient";
import * as languageClientNode from "vscode-languageclient/node";
import * as path from "path";
import * as fs from "fs";

const languageServerPath: string =
  "server/ComponentDesigner.LanguageServer.exe";

let configuration: vscode.WorkspaceConfiguration =
  vscode.workspace.getConfiguration();

let outputChannel = vscode.window.createOutputChannel("Component Designer LSP");

function activateLanguageServer(
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
}

export async function activate(context: vscode.ExtensionContext) {
  const disposable = vscode.commands.registerCommand(
    "extension.helloWorld",
    () => {
      // The code you place here will be executed every time your command is executed

      // Display a message box to the user
      vscode.window.showInformationMessage("Hello World!");
    },
  );

  context.subscriptions.push(disposable);

  outputChannel.appendLine("activiating...");

  activateLanguageServer(context);
  outputChannel.appendLine("CX extension has been activated");
}

export function deactivate() {}
