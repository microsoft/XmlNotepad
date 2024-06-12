## Analytics

To understand which features are important, XML Notepad records anonymous usage data, including:

- The number of times the app is used.
- The frequency of each feature's usage (e.g., options, XSLT, search, etc.).

This data is collected via Google Analytics and shared solely with the XML Notepad development team. It's utilized to prioritize future enhancements based on the popularity of features.

As a sneak peek, here's a map illustrating the countries where XML Notepad was utilized in July 2022. This vibrant user community is a strong motivator for the team to continually refine XML Notepad.

![map](../assets/images/map.png)

Here's some raw data from August 2022 to August 2021:

| Action          | Counts     | Description                                 |
|-----------------|------------|---------------------------------------------|
| /App/Launch     | 1,082,553  | Times app was launched                      |
| /App/XsltView   | 140,703    | Times XSLT view was used                   |
| /App/FormSearch | 83,020     | Times the Search dialog was used            |
| /App/FormOptions| 4,448      | Times the Options dialog was used           |
| /XmlNotepad/    | 208,520    | Visits to the web home page                 |
| /XmlNotepad/install/ | 78,812 | Visits to the install page                  |

Upon the initial installation of XML Notepad on your computer, you will encounter the following dialog:

![popup](../assets/images/analytics.png)

We appreciate those who allow analytics to be collected; it significantly aids in understanding usage patterns and prioritizing improvements. However, if you prefer not to participate, selecting `No` will store your preference in the XML Notepad settings file, and the prompt will not reappear. You can enable or disable analytics at any time using the `Allow analytics` option in the [Options dialog](options.md).

For enterprise-wide distributions or pre-installation customization, you can set the `XML_NOTEPAD_DISABLE_ANALYTICS` environment variable system-wide on end-user machines:


```
set XML_NOTEPAD_DISABLE_ANALYTICS=1
```

Additionally, you can disable the Analytics UI option from appearing in the [Options dialog](options.md) by setting the `AnalyticsClientId` to `disabled` in the default [XmlNotepad.settings](settings.md) file:
```xml
<Settings>
  <AnalyticsClientId>disabled</AnalyticsClientId>
  <AllowAnalytics>False</AllowAnalytics>
</Settings>
```

This ensures that users cannot enable analytics after starting XML Notepad.
