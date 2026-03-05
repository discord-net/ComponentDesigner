package dev.discordnet.componentdesigner

import com.intellij.execution.configurations.PathEnvironmentVariableUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.LspServerSupportProvider
import java.io.File

class CXLspServerSupportProvider : LspServerSupportProvider {
    override fun fileOpened(
        project: Project,
        file: VirtualFile,
        serverStarter: LspServerSupportProvider.LspServerStarter
    ) {
        if (file.fileType != CXFileType) return

        val configService = CXSettings.getService(project)

        val lspExecutable = File(configService.getLspPath())

        if (!lspExecutable.exists()) return

        serverStarter.ensureServerStarted(CXLspServerDescriptor(project, lspExecutable))
    }
}

fun findGlobalLspExecutable(): File? =
    PathEnvironmentVariableUtil.findExecutableInPathOnAnyOS("ComponentDesigner.LanguageServer");