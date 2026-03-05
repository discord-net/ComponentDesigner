package dev.discordnet.componentdesigner.highlighting


import com.google.common.io.Files
import com.intellij.ide.plugins.PluginManagerCore
import com.intellij.openapi.extensions.PluginId
import com.intellij.util.containers.Interner
import org.jetbrains.plugins.textmate.language.TextMateLanguageDescriptor
import org.jetbrains.plugins.textmate.language.syntax.TextMateSyntaxTable
import org.jetbrains.plugins.textmate.language.syntax.lexer.TextMateHighlightingLexer
import java.io.*
import java.nio.file.Path
import java.util.zip.ZipInputStream
import com.intellij.psi.tree.IElementType
import dev.discordnet.componentdesigner.CXFileType
import org.jetbrains.plugins.textmate.bundles.readTextMateBundle
import org.jetbrains.plugins.textmate.language.syntax.lexer.TextMateElementType

private fun deleteFile(file: File) {
    val children = file.listFiles()
    if (children != null) {
        for (child in children) {
            deleteFile(child)
        }
    }
    file.delete()
}

private fun getBundlePath(): Path {
    try {
        val plugin = PluginManagerCore.getPlugin(PluginId.getId("dev.discordnet.ComponentDesigner"))
        val version = plugin?.version ?: "devel"
        val bundleDirectory = File(plugin?.pluginPath.toString() + "/bundles/" + version)
        if (!bundleDirectory.exists()) {
            deleteFile(bundleDirectory.getParentFile())
            bundleDirectory.mkdirs()

            File(bundleDirectory, "Syntaxes").mkdirs()

            CXFileType::class.java.classLoader.getResourceAsStream("tm-bundle/cx.tmLanguage").use { stream ->
                File(bundleDirectory, "/Syntaxes/cx.tmLanguage").outputStream().use { stream?.copyTo(it) }
            }

            CXFileType::class.java.classLoader.getResourceAsStream("tm-bundle/info.plist").use { stream ->
                File(bundleDirectory, "info.plist").outputStream().use { stream?.copyTo(it) }
            }
        }
        return Path.of(bundleDirectory.path)
    } catch (ex: IOException) {
        throw UncheckedIOException(ex)
    }
}
fun getTextMateLanguageDescriptor(): TextMateLanguageDescriptor {
    try {
        val bundle = readTextMateBundle(getBundlePath())
        val syntax = TextMateSyntaxTable()
        val interner = Interner.createWeakInterner<CharSequence>()
        val grammars = bundle.readGrammars()
        for (g in grammars) {
            syntax.loadSyntax(g.plist.value, interner)
        }
        return TextMateLanguageDescriptor("source.cx", syntax.getSyntax("source.cx"))
    } catch (ex: Exception) {
        throw RuntimeException(ex)
    }
}

class CXHighlightingLexer : TextMateHighlightingLexer(getTextMateLanguageDescriptor(), 20000) {
    override fun getTokenType(): IElementType? {
        val tt = super.getTokenType() ?: return null
        return CXElementType((tt as TextMateElementType).scope)
    }
}