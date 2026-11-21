using System.Xml;
using System.Xml.Schema;
using Microsoft.UI.Xaml;
using XmlNotepad.Services;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class SchemaManagerDialog
{
    private XmlSchemaSet? _generationSchemas;
    private string _generationNamespace = string.Empty;

    private async void GenerateSchema_Click(object sender, RoutedEventArgs e) => await RunOperationAsync(async () =>
    {
        if (SingleSelectedSchema is not { } item)
        {
            return;
        }
        var schemas = await Task.Run(() =>
        {
            var resolver = SchemaSource.CreateResolver(item.Location, App.Settings.Resolver);
            var schema = SchemaSource.Read(item.Location, resolver);
            var set = new XmlSchemaSet { XmlResolver = resolver };
            set.Add(schema);
            set.Compile();
            return set;
        });
        if (_closed)
        {
            return;
        }
        var roots = schemas.GlobalElements.Values.OfType<XmlSchemaElement>()
            .Where(element => !element.IsAbstract && element.QualifiedName.Namespace == item.TargetNamespace)
            .Select(element => element.QualifiedName).OrderBy(name => name.Name, StringComparer.Ordinal).ToArray();
        if (roots.Length == 0)
        {
            throw new XmlSchemaException("This schema has no concrete top-level elements.");
        }
        _generationSchemas = schemas;
        _generationNamespace = item.TargetNamespace;
        RootElementBox.ItemsSource = roots.Select(root => root.Name).ToArray();
        RootElementBox.SelectedIndex = 0;
        GenerationOptions.Visibility = Visibility.Visible;
        RootElementBox.Focus(FocusState.Programmatic);
    });

    private async void CreateGeneratedDocument_Click(object sender, RoutedEventArgs e) => await RunOperationAsync(async () =>
    {
        if (_generationSchemas is not { } schemas || RootElementBox.SelectedItem is not string rootName)
        {
            return;
        }
        var name = new XmlQualifiedName(rootName, _generationNamespace);
        var document = await Task.Run(() =>
        {
            var generator = new XsdInstanceGenerator(schemas, new XmlNamespaceManager(new NameTable()));
            return generator.Generate(name) ?? throw new XmlSchemaException($"Could not generate an instance for '{name.Name}'.");
        });
        if (!_closed)
        {
            App.OpenDocumentWindow(null, initialDocument: document);
        }
    });
}
