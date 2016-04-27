<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:user="foo">
  <msxsl:script language="C#"   implements-prefix="user"><![CDATA[
    public string GetMessage() {
       return "The script executed successfully.";
    }
  ]]></msxsl:script>
<xsl:template match="/">
  <html>
    <h3>
      Found <xsl:value-of select="count(rss/channel/item)"/> RSS items. 
      <xsl:value-of select="user:GetMessage()"/>
    </h3>
  </html>
</xsl:template>
</xsl:stylesheet> 
