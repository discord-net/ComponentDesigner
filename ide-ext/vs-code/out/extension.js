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
const jsonPreviewProvider_1 = require("./jsonPreviewProvider");
const languageServerPath = "server/ComponentDesigner.LanguageServer.exe";
let configuration = vscode.workspace.getConfiguration();
const discordPreviewPanels = new Map();
let outputChannel = vscode.window.createOutputChannel("Component Designer LSP");
function activateLanguageServer(context) {
    return __awaiter(this, void 0, void 0, function* () {
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
        yield client.onReady();
        context.subscriptions.push(client.onNotification("cx/preview-json", (params) => {
            const uri = vscode.Uri.parse(params.uri);
            outputChannel.appendLine(`Json for ${uri.path}\n${params.json}`);
            jsonPreviewProvider_1.default.updateContent(uri, params.json);
            const discordPanel = discordPreviewPanels.get(uri.toString());
            outputChannel.appendLine("discord preview panel exists: " + !!discordPanel);
            if (params.success) {
                discordPanel === null || discordPanel === void 0 ? void 0 : discordPanel.webview.postMessage({
                    type: 'updateComponents',
                    components: JSON.parse(params.json),
                });
            }
        }));
        return client;
    });
}
function activate(context) {
    return __awaiter(this, void 0, void 0, function* () {
        outputChannel.appendLine("activiating...");
        yield activateLanguageServer(context);
        context.subscriptions.push(vscode.workspace.registerTextDocumentContentProvider("cx-preview", jsonPreviewProvider_1.default));
        context.subscriptions.push(vscode.commands.registerCommand("cx.openPreview", () => __awaiter(this, void 0, void 0, function* () {
            if (!vscode.window.activeTextEditor) {
                return; // no editor
            }
            let { document } = vscode.window.activeTextEditor;
            if (document.languageId !== "cx")
                return;
            outputChannel.appendLine("Opening preview for: " + document.uri.toString());
            const previewUri = vscode.Uri.parse(`cx-preview:${document.uri.path}`);
            const doc = yield vscode.workspace.openTextDocument(previewUri);
            vscode.languages.setTextDocumentLanguage(doc, "json");
            yield vscode.window.showTextDocument(doc, {
                viewColumn: vscode.ViewColumn.Beside,
                preserveFocus: true,
            });
        })));
        context.subscriptions.push(vscode.commands.registerCommand("cx.discordPreview", () => __awaiter(this, void 0, void 0, function* () {
            if (!vscode.window.activeTextEditor) {
                return; // no editor
            }
            let { document } = vscode.window.activeTextEditor;
            if (document.languageId !== "cx")
                return;
            const panel = vscode.window.createWebviewPanel("cx-discord-preview", "Discord Preview", vscode.ViewColumn.Beside, {
                localResourceRoots: [
                    vscode.Uri.joinPath(context.extensionUri, "discord-preview"),
                ],
                enableScripts: true,
            });
            panel.onDidDispose(() => {
                discordPreviewPanels.delete(document.uri.toString());
            });
            discordPreviewPanels.set(document.uri.toString(), panel);
            const path = context.asAbsolutePath("discord-preview/index.html");
            outputChannel.appendLine("Loading Discord preview HTML from: " + path);
            fs.readFile(path, { encoding: "utf-8" }, (err, data) => {
                if (err) {
                    outputChannel.appendLine("Error loading Discord preview HTML: " + err.message);
                    return;
                }
                const html = data.replaceAll("EXT_PATH_PREFIX", panel.webview
                    .asWebviewUri(vscode.Uri.joinPath(context.extensionUri, "discord-preview"))
                    .toString());
                panel.webview.html = html;
                outputChannel.appendLine("Discord preview opened for: " + document.uri.toString());
            });
            // context.subscriptions.push(
            //   jsonPreviewProvider.onDidChange((uri) => {
            //     const panel = discordPreviewPanels.get(uri);
            //     if (!panel) return;
            //     const json = jsonPreviewProvider.provideTextDocumentContent(uri);
            //     outputChannel.appendLine("Updated panel json for: " + uri.toString());
            //     outputChannel.appendLine(json);
            //   }),
            // );
        })));
        outputChannel.appendLine("CX extension has been activated");
    });
}
function deactivate() { }
//# sourceMappingURL=extension.js.map