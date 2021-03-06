using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.CommandBars;

/*
interesting face ids

javascript:
	588 -- script scroll
	629 -- script scroll with 'ding' star
	1839 - another scroll
html:
	610 -- world icon
	1445 - IE with page 
	3763 - yellow page w/ world on it and + sign
funny:
	643 -- POW!
misc:
    504 -- paragraph groupings
	680 -- card with 'ding' star
	1544 - a page
	2646 - page with + sign
	3631 - VS 2005 icon
	3778 - F2 key
	3811 - page w/ blue squigles and 'ding' star
 */
public class VSNewFile : IDTExtensibility2, IDTCommandTarget {
    private DTE2 _applicationObject;
    private AddIn _addInInstance;
    private static string _newFileTemplatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VSNewFile Templates");
    private static Dictionary<String, TemplateInfo> _templates;
    private static List<String> _acceptableTargets = new List<string> {
        EnvDTE.Constants.vsProjectItemKindPhysicalFolder,
        VSLangProj2.PrjKind2.prjKindSDECSharpProject,
        VSLangProj2.PrjKind2.prjKindSDEVBProject,
        VSLangProj2.PrjKind2.prjKindVJSharpProject
    };

    private static Dictionary<String, TemplateInfo> GetDefaultTemplates() {
        return new Dictionary<String, TemplateInfo> {
            {"FILE", new TemplateInfo {
                Key = "FILE",
                Title = "File",
                BaseName = "file", Extention = null,
                FaceID = 643,
                Position = 1
            }},
            {"CS", new TemplateInfo {
                Key = "CS",
                Title = "C# Class",
                BaseName = "class", Extention = "cs",
                FaceID = 504,
                Position = 2,
                Content = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace %NAMESPACE% {
    public class %FILENAME% {
    }
}
"
            }},
            {"VB", new TemplateInfo {
                Key = "VB",
                Title = "VB Class",
                BaseName = "class", Extention = "vb",
                FaceID = 439,
                Position = 3,
                Content = @"Public Class Class1

End Class
"
            }},
            {"HTML", new TemplateInfo {
                Key = "HTML",
                Title = "HTML page",
                BaseName = "page", Extention = "html",
                FaceID = 610,
                Position = 4,
                Content = @"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">
<html>
	<head>
		<title></title>
	</head>
	<body>
	
	</body>
    <script src="""" type=""text/javascript""></script>
    <script type=""text/javascript"">
    //<![CDATA[

    ///]]>
    </script>
</html>"
            }},
            {"JS", new TemplateInfo {
                Key = "JS",
                Title = "Script",
                BaseName = "script", Extention = "js",
                FaceID = 629,
                Position = 5,
                Content = @"
(function() {


})();
"
            }}
        };
    }

    private static Dictionary<String, TemplateInfo> Templates {
        get {
            if (_templates == null) {
                var templates = GetDefaultTemplates();

                if (Directory.Exists(_newFileTemplatePath)) {
                    foreach (string filePath in Directory.GetFiles(_newFileTemplatePath)) {
                        // Position_KEY_BaseName_FaceID_Title.Extention
                        // Disable: _KEY
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        if (fileName.StartsWith("_")) {
                            string key = fileName.Substring(1);
                            TemplateInfo template;
                            if (templates.TryGetValue(key, out template)) {
                                templates.Remove(key);
                            }
                        }
                        else {
                            string extention = Path.GetExtension(filePath).TrimStart('.');
                            string[] parts = fileName.Split('_');
                            int position;
                            if (parts.Length > 1 && int.TryParse(parts[0], out position)) {
                                string key = parts[1];
                                string name = parts.Length > 2 ? parts[2] : "new";
                                int faceID = 643;
                                if (parts.Length > 3) {
                                    int.TryParse(parts[3], out faceID);
                                }
                                string title = parts.Length > 4 ? parts[4] : (" " + extention);
                                string content = File.ReadAllText(filePath);
                                if (templates.ContainsKey(key)) {
                                    templates.Remove(key);
                                }
                                templates[key] = new TemplateInfo {
                                    Key = key,
                                    BaseName = name,
                                    FaceID = faceID,
                                    Title = title,
                                    Extention = extention,
                                    Position = position,
                                    Content = content
                                };
                            }
                        }
                    }
                }
                _templates = templates;
            }
            return _templates;
        }
    }

    private void AddItemCommand(string name, string text, object bindings, int position, int faceId, bool beginGroup) {
        object[] contextGUIDS = new object[] { };
        Commands2 commands = (Commands2)_applicationObject.Commands;
        CommandBars commandBars = ((CommandBars)_applicationObject.CommandBars);
        CommandBar[] targetBars = new CommandBar[] {
            commandBars["Folder"],
            commandBars["Project"],
            commandBars["Web Folder"],
            commandBars["Web Project Folder"]
        };

        try {
            Command command = commands.AddNamedCommand2(_addInInstance, name, text, null, true, faceId, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
            if (bindings != null) {
                command.Bindings = bindings;
            }
            if (command != null) {
                foreach (CommandBar bar in targetBars) {
                    CommandBarButton button = (CommandBarButton)command.AddControl(bar, position);
                    button.BeginGroup = beginGroup;
                }
            }
        }
        catch (ArgumentException) {
        }
    }

    public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom) {
        _applicationObject = (DTE2)application;
        _addInInstance = (AddIn)addInInst;

        if (connectMode == ext_ConnectMode.ext_cm_Startup) {
            var bindings = new Dictionary<string, object>();
            for (var i = 1; i <= _applicationObject.Commands.Count; i++) {
                Command command = _applicationObject.Commands.Item(i);
                string name = command.Name;
                if (name.StartsWith("VSNewFile.New", StringComparison.Ordinal)) {
                    name = name.Substring("VSNewFile.New".Length);
                    bindings.Add(name, command.Bindings);
                }
            }
            foreach (var pair in bindings) {
                _applicationObject.Commands.Item("VSNewFile.New" + pair.Key).Delete();
            }

            bool first = true;
            int position = 1;
            foreach (var pair in Templates.OrderBy(t => t.Value.Position)) {
                object keyBinding;
                bindings.TryGetValue(pair.Key, out keyBinding);
                AddItemCommand("New" + pair.Key, "Add " + pair.Value.Title, keyBinding, position++, pair.Value.FaceID, first);
                first = false;
            }

        }
    }

    public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom) {
        if (disconnectMode == ext_DisconnectMode.ext_dm_UserClosed) {
            // user has turned off the addin, remove all the commands
            List<Command> toDelete = new List<Command>();
            for (var i = 1; i <= _applicationObject.Commands.Count; i++) {
                Command command = _applicationObject.Commands.Item(i);
                string name = command.Name;
                if (name.StartsWith("VSNewFile.New", StringComparison.Ordinal)) {
                    toDelete.Add(command);
                }
            }
            foreach (Command command in toDelete) {
                command.Delete();
            }
        }
    }

    public void OnAddInsUpdate(ref Array custom) {
    }

    public void OnStartupComplete(ref Array custom) {
    }

    public void OnBeginShutdown(ref Array custom) {
    }

    public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText) {
        if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone) {
            if (commandName.StartsWith("VSNewFile.New")) {
                status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported | vsCommandStatus.vsCommandStatusEnabled;
                return;
            }
        }
    }

    public void AddFile(TemplateInfo template) {
        if (_applicationObject.SelectedItems.Count == 1) {
            var selectedItem = _applicationObject.SelectedItems.Item(1);
            string targetPath = null;
            Project project = selectedItem.Project ?? (selectedItem.ProjectItem != null ? selectedItem.ProjectItem.ContainingProject : null);
            if (selectedItem.ProjectItem == null) {
                if (project != null) {
                    // project is selected, add it to the top level of the project
                    targetPath = (string)project.Properties.Item("FullPath").Value;
                }
            }
            else {
                // an item within the project is probably selected
                object parent = selectedItem.ProjectItem;
                while (parent != null && !_acceptableTargets.Contains(GetKind(parent))) {
                    parent = GetParent(parent);
                }
                if (parent != null) {
                    if (parent is ProjectItem) {
                        targetPath = (string)((ProjectItem)parent).Properties.Item("FullPath").Value;
                    }
                    else if (parent is Project) {
                        targetPath = (string)((Project)parent).Properties.Item("FullPath").Value;
                    }
                }
            }
            if (!String.IsNullOrEmpty(targetPath)) {
                string path = Path.Combine(targetPath, template.BaseName + (String.IsNullOrEmpty(template.Extention) ? "" : ("." + template.Extention)));
                int i = 1;
                while (File.Exists(path)) {
                    path = Path.Combine(targetPath, template.BaseName + i++ + (String.IsNullOrEmpty(template.Extention) ? "" : ("." + template.Extention)));
                }
                string ns = null;
                try {
                    ns = (string)project.Properties.Item("DefaultNamespace").Value;
                }
                catch(ArgumentException) {
                    // probably a 'web site' project. Determine namespace from the website name instead.
                    ns = project.FullName.TrimEnd('\\');
                    ns = ns.Split('\\').Last() ?? "Namespace";
                }
                File.WriteAllText(path, template.GetContent(ns, Path.GetFileNameWithoutExtension(path)));
                var newItem = project.ProjectItems.AddFromFile(path);
                // make it selected in solution explorer
                _applicationObject.ToolWindows.SolutionExplorer.GetItem(GetSolutionPath(newItem, true)).Select(vsUISelectionType.vsUISelectionTypeSelect);
                // now start renaming it to the user can give it a proper name
                _applicationObject.ExecuteCommand("File.Rename");
            }
            else {
                MessageBox.Show("Unable to locate folder to add the item to.");
            }
        }
    }

    public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled) {
        handled = false;
        if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault) {
            if (commandName.StartsWith("VSNewFile.New")) {
                commandName = commandName.Substring("VSNewFile.New".Length);
            }
            TemplateInfo template;
            if (Templates.TryGetValue(commandName, out template)) {
                handled = true;
                AddFile(template);
            }
        }
    }

    private string GetKind(object target) {
        if (target is ProjectItem) {
            return ((ProjectItem)target).Kind;
        }
        else if (target is Project) {
            return ((Project)target).Kind;
        }
        else if (target is Solution) {
            return EnvDTE.Constants.vsProjectsKindSolution;
        }
        return null;
    }

    private string GetName(object target) {
        string name = null;
        if (target is ProjectItem) {
            name = ((ProjectItem)target).Name;
        }
        else if (target is Project) {
            name = ((Project)target).Name;
        }
        else if (target is Solution) {
            name = (string)_applicationObject.Solution.Properties.Item("Name").Value;
        }
        if (name != null) {
            name = name.TrimEnd('\\').Split('\\').Last();
        }
        return name;
    }

    private object GetParent(object target) {
        if (target is ProjectItem) {
            return ((ProjectItem)target).Collection.Parent;
        }
        else if (target is Project) {
            var parent = ((Project)target).Collection.Parent;
            if (parent == _applicationObject) {
                return _applicationObject.Solution;
            }
            else {
                return parent;
            }
        }
        return null;
    }

    private string GetSolutionPath(ProjectItem target, bool includingSolution) {
        string path = target.Name;
        object item = target;
        do {
            item = GetParent(item);
            if (item != null && (includingSolution || (GetKind(item) != EnvDTE.Constants.vsProjectsKindSolution))) {
                path = GetName(item) + "\\" + path;
            }
        }
        while (item != null && GetKind(item) != EnvDTE.Constants.vsProjectsKindSolution);
        return path.TrimStart('\\');
    }
}
