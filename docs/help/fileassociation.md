## XML Notepad File Association

Watch the following video tutorial to learn how to associate XML Notepad with various .xml file types:

[YouTube Video](https://youtu.be/n-6sSUSlN34)

Here's a step-by-step guide:

1. Open XML Notepad, which you have already installed.
2. Navigate to Help > Open Sample... and select "Hamlet.xml".
3. Note the long file path displayed in the address bar, such as:

``` mathematica
C:\Program Files\WindowsApps\43906ChrisLovett.XmlNotepad_2.9.0.5_neutral__hndwmj480pefj\Application\Samples\Hamlet.xml
```

If you open this path in Windows Explorer you can locate XmlNotepad.exe here:

``` mathematica
> C:\Program Files\WindowsApps\43906ChrisLovett.XmlNotepad_2.9.0.5_neutral__hndwmj480pefj\Application\XmlNotepad.exe
```

Copy this path to the clipboard.

Now, follow these steps:

- Open the "Default apps" settings dialog using the start menu.
- Enter the file type at the top (e.g., type in ".xsd" or ".xslt").
- Click the button labeled `+ Choose a default`.
- Scroll to the bottom of this dialog and click `Choose an app on your PC`.
- Paste in the path you copied to the clipboard earlier.
- Click `Open`, then click `Set Default`.

As soon as you do this, the icon on the file type you associated should change in Windows Explorer, showing the XML Notepad icon, and when you double click the file, it will automatically open XML Notepad.

Note that when you have done one extension, all the other extensions are much easier because XML Notepad will now show up in the list of possible apps to choose.

Keep in mind that the path you have registered here is version specific, so you may have to repeat this process when you install new versions of XML Notepad.
