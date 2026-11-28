using System.Xml;

namespace XmlNotepad.Services;

internal static class XmlNodeReplacement
{
    public static void Replace(XmlNode from, XmlNode to)
    {
        if (from is XmlAttribute sourceAttribute && to is XmlAttribute targetAttribute)
        {
            var owner = sourceAttribute.OwnerElement!;
            var index = 0;
            while (owner.Attributes[index] != sourceAttribute)
            {
                index++;
            }
            owner.Attributes.Remove(sourceAttribute);
            owner.Attributes.InsertBefore(targetAttribute, index < owner.Attributes.Count ? owner.Attributes[index] : null);
        }
        else if (from is XmlElement sourceElement && to is XmlElement targetElement)
        {
            targetElement.IsEmpty = sourceElement.IsEmpty;
            foreach (var attribute in sourceElement.Attributes.Cast<XmlAttribute>().ToArray())
            {
                sourceElement.Attributes.Remove(attribute);
                targetElement.Attributes.Append(attribute);
            }
        }

        if (from is XmlElement or XmlAttribute)
        {
            while (from.FirstChild is { } child)
            {
                from.RemoveChild(child);
                to.AppendChild(child);
            }
        }

        if (from is not XmlAttribute)
        {
            from.ParentNode!.ReplaceChild(to, from);
        }
    }
}
