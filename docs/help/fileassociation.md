## XML Notepad File Association

See the following video that shows how to associate XML Notepad with various .xml file types:

[Youtube Video](https://youtu.be/n-6sSUSlN34)

The general approach that works is to do the following:

1. Open the XML Notepad that you installed already.
2. Select Help/Open Sample... and select "Hamlet.xml"
3. You will see a long file path in the address bar like this:

> C:\Program Files\WindowsApps\43906ChrisLovett.XmlNotepad_2.9.0.5_neutral__hndwmj480pefj\Application\Samples\Hamlet.xml

If you open this path in Windows Explorer you can locate XmlNotepad.exe here:

> C:\Program Files\WindowsApps\43906ChrisLovett.XmlNotepad_2.9.0.5_neutral__hndwmj480pefj\Application\XmlNotepad.exe

Copy this path to the clipboard.

Now use the start menu to find the "Default apps" settings dialog.

Enter the file type at the top (e.g. type in ".xsd" or ".xslt"),
then click the button labelled `+ Choose a default`.

Scroll to the bottom of this dialog and click `Choose an app on your PC`

And paste in the path you coped to the clipboard earlier.
Click `Open` then click `Set Default`.

As soon as you do this the icon on the file type you associated  should change
in Windows Explorer showing the XML Notepad icon and when you double click the
file it will automatically open XML Notepad.

Note that when you have done one extension, all the other extensions are much
easier because XML Notepad will now show up in the list of possible apps to
choose.

The path you have registered here is version specific so you may have to repeat
this process when you install new versions of XML Notepad.