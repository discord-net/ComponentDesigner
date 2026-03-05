package dev.discordnet.componentdesigner

import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project

class CXState {
    var lspPath = ""
}

@State(name = "CXSettings", storages = [Storage("cx.xml")])
class CXSettings : PersistentStateComponent<CXState> {
    companion object {
        fun getService(project: Project): CXSettings = project.service<CXSettings>()
    }

    private var state = CXState()

    override fun getState(): CXState {
        return state
    }

    override fun loadState(state: CXState) {
        this.state = state
    }

    fun getLspPath(): String {
        val path = this.state.lspPath

        if (path.isEmpty()) {
            return findGlobalLspExecutable()?.absolutePath ?: ""
        }

        return path
    }
}