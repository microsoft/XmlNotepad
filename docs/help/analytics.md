
## Analytics

In order to understand which features are important XML Notepad records anonymous usage like:

1. how many times the app is used
2. how many times each feature is used (options, xslt, search, etc)

The data is collected using Google Analytics and shared only with members of the XML Notepad
development team. The data is used to prioritize future work to improve the features that are most
popular.

As a teaser, here is a map of countries where XML Notepad was used in July 2021.  That's super cool
to see a healthy community of users and this motivates the team to keep XML notepad in tip-top
shape.

![map](../assets/images/map.png)

The first time you install XML Notepad on your computer you will be prompted with the following
dialog:

![popup](../assets/images/analytics.png)

We hope you don't but if you really need to click `No` then the choice will be written to the XML
Notepad settings file and it will not be prompted again. Analytics can be enabled or disabled any
time using the `Allow analytics` option in the [Options dialog](options.md).

If you want to disable analytics before installing XML Notepad (perhaps in an enterprise wide
distribution) then you can set this environment system wide on the end user's machine:

```
set XML_NOTEPAD_DISABLE_ANALYTICS=1
```

You can also disable the Analytics UI option from appearing in the [Options dialog](options.md)
by setting the `AnalyticsClientId` to disabled in the default XmlNotepad.settings file located here:

```
%LOCALAPPDATA%\Microsoft\Xml Notepad\XmlNotepad.settings
```
as follows:
```xml
<Settings>
  <AnalyticsClientId>disabled</AnalyticsClientId>
  <AllowAnalytics>False</AllowAnalytics>
</Settings>
```

This ensures the user cannot enable analytics after starting XML notepad.
