package dev.discordnet.componentdesigner.highlighting

import com.intellij.psi.tree.IElementType
import dev.discordnet.componentdesigner.CXLanguage
import org.jetbrains.plugins.textmate.language.syntax.lexer.TextMateScope

class CXElementType(private val scope: TextMateScope) : IElementType("TEMPL_TOKEN", CXLanguage, false) {
    private val myScope = scope

    fun getScope(): TextMateScope {
        return myScope
    }

    override fun hashCode(): Int {
        return getScope().hashCode()
    }

    override fun toString(): String {
        return myScope.toString()
    }

    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other == null || this.javaClass != other.javaClass) return false
        return (other as CXElementType).getScope() == this.getScope()
    }
}