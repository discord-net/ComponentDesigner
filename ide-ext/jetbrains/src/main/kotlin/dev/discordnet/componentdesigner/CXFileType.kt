package dev.discordnet.componentdesigner

import com.intellij.openapi.fileTypes.LanguageFileType
import com.intellij.ui.icons.EMPTY_ICON
import javax.swing.Icon

object CXFileType : LanguageFileType(CXLanguage) {
    override fun getName(): String = "cx"

    override fun getDescription(): String = "The syntax used for the Component Designer (CX)"

    override fun getDefaultExtension(): String = "cx"

    override fun getIcon(): Icon = EMPTY_ICON
}