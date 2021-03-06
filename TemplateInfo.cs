using System;
using EnvDTE;

public class TemplateInfo {
    public string Key { get; set; }
    public string BaseName { get; set; }
    public string Extention { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public int FaceID { get; set; }
    public int Position { get; set; }

    public string GetContent(string classNamespace, string fileName) {
        string content = Content;
        if (!String.IsNullOrEmpty(content)) {
            content = content
                .Replace("%NAMESPACE%", classNamespace)
                .Replace("%FILENAME%", fileName);
        }
        return content;
    }
}
