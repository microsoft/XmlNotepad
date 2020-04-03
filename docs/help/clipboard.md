---
layout: default
title: Overview
section: home
permalink: /help/clipboard
---


## Clipboard

Cut/Copy/Paste and Drag/drop are based on the same XML clipboard format. For example, you can drag a node out of Microsoft XML Notepad and into any editor that supports a text clipboard format and its the same as doing a Cut & Paste operation.

The clipboard format is the XML serialization of the selected node plus any namespace declarations needed to make that fragment well formed.

When XML is pasted from the clipboard (or dropped) onto the tree view the namespace declarations are matched with what is in the target document already and normalized accordingly. This ensures your document doesn't sprout lots of redundant namespace declarations as a side effect of these editing operations.