package dev.discordnet.componentdesigner

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.lsp.api.ProjectWideLspServerDescriptor
import java.io.File

class CXLspServerDescriptor(project: Project, val lspExecutable: File) :
    ProjectWideLspServerDescriptor(project, "cx") {

    override fun isSupportedFile(file: VirtualFile): Boolean = file.fileType == CXFileType

    override fun createCommandLine(): GeneralCommandLine =
        GeneralCommandLine(lspExecutable.absolutePath)
}