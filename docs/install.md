---
layout: default
title: Home
section: home
permalink: /install
---

## Install XML Notepad

There are thee ways to install XML Notepad:

<div>
<a href="http://www.lovettsoftware.com/downloads/XmlNotepad/setup.exe" class="btn btn-primary mt-20 mr-30" target="_blank">ClickOnce® installer</a>
<br/>
<br/>
</div>

This is the most convenient, install it directly from the web browser.  If the browser downloads this file, click
"Open file" to install it.

If you need something that installs offline for machines that have no network or are isolated from the internet
then use the standalone installer:

<div>
<a href="http://www.lovettsoftware.com/downloads/XmlNotepad/XmlNotepadsetup.zip" class="btn btn-primary mt-20 mr-30" target="_blank">Standalone installer</a>
<br/>
<br/>
</div>

Just download the zip file, copy it to the machine you want to install it on, unzip the file on that machine and run `XmlNotepadSetup.msi`.

<a href="https://docs.microsoft.com/en-us/windows/package-manager/winget/" class="btn btn-primary mt-20 mr-30" target="_blank">winget installer</a>

And you can also use `winget` to install XML Notepad using this command line:

```
winget install XmlNotepad
```
<br/>
<br/>


### Troubleshooting

If you have trouble installing the ClickOnce installer package, try a Microsoft Web Browser.  Chrome sometimes does the wrong thing.  Do not attempt to download the setup.exe from [http://www.lovettsoftware.com/downloads/XmlNotepad/setup.exe](http://www.lovettsoftware.com/downloads/XmlNotepad/setup.exe) and install it offline.

ClickOnce only works when you "click" the link in the web browser.  This is actually a security feature.  ClickOnce is verifying the that package it is installing actually came from [http://www.lovettsoftware.com/downloads/XmlNotepad/setup.exe](http://www.lovettsoftware.com/downloads/XmlNotepad/setup.exe) and nowhere else.  This ensures the package has not been tampered with.
