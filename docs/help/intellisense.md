---
layout: default
title: Overview
section: home
permalink: /help/intellisense
---

## Intellisense

If an XML schema is provided for validation then this same schema is also used to provide intellisense. For example, if your schema defines an `<xsd:choice>` between the elements "fruit", "vegetable" and "berry", then when you insert a new node at this location you will be prompted with those choices as shown below:

![DragDrop](/XmlNotepad/assets/images/intellisense.jpg)

Similarly, if your schema defines a simpleType with a list of `<xsd:enumeration>` facets, then this list of valid values is prompted when you edit the node value as follows:

![DragDrop](/XmlNotepad/assets/images/intellisense2.jpg)

Then as you type the item matching the letters you have typed so far will be automatically selected in the drop down list and when you type ENTER the selected item is copied to your edit field and the intellisense selection is finished which saves you from having to type the whole string in manually. This can result in much more efficient editing of XML documents.

XML Notepad also supports special [custom editors](/XmlNotepad/help/customeditors) for well known data types like date, time and color.

See also [Schemas Dialog](/XmlNotepad/help/schemas)