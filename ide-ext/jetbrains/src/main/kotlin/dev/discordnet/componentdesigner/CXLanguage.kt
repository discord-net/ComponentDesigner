package dev.discordnet.componentdesigner

import com.intellij.lang.Language
import com.intellij.openapi.util.NlsSafe

object CXLanguage : Language("cx") {
    override fun getDisplayName(): String = "Component Designer"
}