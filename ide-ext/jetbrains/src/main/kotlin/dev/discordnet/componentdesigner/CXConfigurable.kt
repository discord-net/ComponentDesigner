package dev.discordnet.componentdesigner

import com.intellij.openapi.options.Configurable
import com.intellij.openapi.options.ConfigurationException
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.TextFieldWithBrowseButton
import com.intellij.openapi.util.NlsContexts.ConfigurableName
import com.intellij.util.ui.FormBuilder
import java.awt.BorderLayout
import javax.swing.JCheckBox
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.JTextField

class CXConfigurable(private val project: Project) : Configurable {

    private val myLspPath = TextFieldWithBrowseButton()


    override fun getDisplayName(): @ConfigurableName String {
        return "CX"
    }

    override fun createComponent(): JComponent {
        val service = CXSettings.getService(project)

        val mainFormBuilder = FormBuilder.createFormBuilder()
        mainFormBuilder.addLabeledComponent("LSP Path", myLspPath)
        val wrapper = JPanel(BorderLayout())
        wrapper.add(mainFormBuilder.panel, BorderLayout.NORTH)
        return wrapper
    }

    override fun isModified(): Boolean {
        val service = CXSettings.getService(project)
        val state = service.state

        return myLspPath.text != state.lspPath
    }

    @Throws(ConfigurationException::class)
    override fun apply() {
        if (isModified) {
            val service = CXSettings.getService(project)
            val state = service.state
            state.lspPath = myLspPath.text
            service.loadState(state)
        }
    }
}