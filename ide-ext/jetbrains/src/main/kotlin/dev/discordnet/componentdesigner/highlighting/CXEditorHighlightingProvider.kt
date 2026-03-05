package dev.discordnet.componentdesigner.highlighting

import com.intellij.openapi.editor.colors.EditorColorsScheme
import com.intellij.openapi.editor.ex.util.DataStorage
import com.intellij.openapi.editor.ex.util.LexerEditorHighlighter
import com.intellij.openapi.editor.highlighter.EditorHighlighter
import com.intellij.openapi.fileTypes.EditorHighlighterProvider
import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.fileTypes.SyntaxHighlighter
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile

class CXEditorHighlighterProvider : EditorHighlighterProvider {
    override fun getEditorHighlighter(project: Project?, ft: FileType, virtualFile: VirtualFile?, colors: EditorColorsScheme): EditorHighlighter {
        return CXEditorHighlighter(CXHighlighterFactory().getSyntaxHighlighter(project, virtualFile), colors)
    }

    private class CXEditorHighlighter(highlighter: SyntaxHighlighter, colors: EditorColorsScheme) : LexerEditorHighlighter(highlighter, colors) {
        override fun createStorage(): DataStorage {
            return CXLexerDataStorage()
        }
    }
}