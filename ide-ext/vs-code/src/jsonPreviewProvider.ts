import * as vscode from "vscode";

export default new class implements vscode.TextDocumentContentProvider {
    onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();
	onDidChange = this.onDidChangeEmitter.event;

    docs: Map<string, string> = new Map();

    provideTextDocumentContent(uri: vscode.Uri): string {
		return this.docs.get(uri.path) || "";
	}

    updateContent(uri: vscode.Uri, content: string) {
        const previewUri = vscode.Uri.parse(`cx-preview:${uri.path}`);
        this.docs.set(uri.path, content);
        this.onDidChangeEmitter.fire(previewUri);
    }
}