# Check for updates

Choose **Help > Check for updates**. If a newer version is available, choose **Get update** to open the download page. If you installed with WinGet, you can update from the command line. See [Install XML Notepad](../install.md).

Under **View > Options > Updates**, set whether checks run automatically, where the XML update feed lives, and how often the app checks. For example, `1.00:00:00` is one day. The feed can require a longer interval.

The [release feed and change history](https://github.com/microsoft/XmlNotepad/blob/master/src/Updates/Updates.xml) lists published versions and can supply installation and history links. XML Notepad uses the highest valid version in the feed.

Administrators can set `DisableUpdateUI` to `True` in the [settings file](settings.md) to hide the update controls and stop automatic checks.
