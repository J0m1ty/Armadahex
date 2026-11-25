using UnityEngine;
using UnityEngine.UIElements;

// what's new on the menu, from Resources/Changelog.txt for now (it'll come from the auth server later).
// "# title | date" starts an update, "## heading" a group inside it, "- text" a line
public static class Changelog
{
    public static void Fill(ScrollView list) {
        if (list == null) return;

        var file = Resources.Load<TextAsset>("Changelog");
        if (file == null) return;

        list.Clear();
        foreach (var raw in file.text.Split('\n')) {
            var line = raw.Trim();
            if (line.StartsWith("## ")) {
                list.Add(Label(line.Substring(3).ToUpper(), "changelog__group"));
            }
            else if (line.StartsWith("# ")) {
                var parts = line.Substring(2).Split('|');
                var head = new VisualElement();
                head.AddToClassList("changelog__update");
                head.Add(Label(parts[0].Trim().ToUpper(), "changelog__title"));
                if (parts.Length > 1) head.Add(Label(parts[1].Trim().ToUpper(), "changelog__date"));
                list.Add(head);
            }
            else if (line.StartsWith("- ")) {
                var row = new VisualElement();
                row.AddToClassList("changelog__line");
                var dot = new VisualElement();
                dot.AddToClassList("changelog__dot");
                row.Add(dot);
                row.Add(Label(line.Substring(2), "changelog__text"));
                list.Add(row);
            }
        }
    }

    private static Label Label(string text, string className) {
        var label = new Label(text);
        label.AddToClassList(className);
        return label;
    }
}
