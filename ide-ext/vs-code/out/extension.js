"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = require("vscode");
const languageClientNode = require("vscode-languageclient/node");
const path = require("path");
const fs = require("fs");
const languageServerPath = "server/ComponentDesigner.LanguageServer.exe";
let configuration = vscode.workspace.getConfiguration();
let outputChannel = vscode.window.createOutputChannel("Component Designer LSP");
function activateLanguageServer(context) {
    let pathFile = context.asAbsolutePath(languageServerPath);
    if (!fs.existsSync(pathFile)) {
        outputChannel.appendLine("Language server not found at path: " + pathFile);
        return;
    }
    let pathDir = path.dirname(pathFile);
    let serverOptions = {
        run: {
            command: pathFile,
            options: { cwd: pathDir },
        },
        debug: {
            command: pathFile,
            options: { cwd: pathDir },
        },
    };
    let clientOptions = {
        documentSelector: ["cx"],
        synchronize: {
            configurationSection: "cx",
        },
    };
    let client = new languageClientNode.LanguageClient("DiscordComponentsLanguageServer", "Discord Components Language Server", serverOptions, clientOptions);
    let disposable = client.start();
    context.subscriptions.push(disposable);
}
function activate(context) {
    return __awaiter(this, void 0, void 0, function* () {
        const disposable = vscode.commands.registerCommand("extension.helloWorld", () => {
            // The code you place here will be executed every time your command is executed
            // Display a message box to the user
            vscode.window.showInformationMessage("Hello World!");
        });
        context.subscriptions.push(disposable);
        outputChannel.appendLine("activiating...");
        activateLanguageServer(context);
        outputChannel.appendLine("CX extension has been activated");
    });
}
function deactivate() { }
//# sourceMappingURL=extension.js.map