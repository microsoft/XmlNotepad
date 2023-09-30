
## Schemas

The "Schemas..." item under the [View Menu](menus.md) brings up the following dialog showing the current set of known XML schemas. These schemas are used for [Validation](validation.md) and [Intellisense](intellisense.md).

![Find](../assets/images/schemas.png)

You can click the browse buttons on the right hand side to bring up the Open File Dialog to browse for new files or you can select the "Add Files" item in the [File Menu](menus.md) to add a batch of schemas. You can click the column headings to sort by that column.

The first column contains check boxes which can be used to temporarily disable a schema from being used in validation.

This list of schemas is persisted in the `XmlNotepad.settings` file so it is remembered next time you load XML Notepad.

When you add schemas this way you can create a new document and
when you add a new root element you should see some intellisense showing possible root elements and when you select from the dropdown the new element is associated with the correct namespace so that intellisense continues from there.