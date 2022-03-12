<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
	xmlns:user="urn:my-scripts">
	<xsl:output method="text"/>
	<msxsl:script language="C#" implements-prefix="user">
		<msxsl:assembly name="System.Web" />
		<msxsl:using namespace="System.Web" />
		<![CDATA[
      public string StripLines(string text)
      {
			string[] lines = text.Split('\n');
			for(int i = 0; i < lines.Length; i++) {
				lines[i] = lines[i].Trim();
			}
			return string.Join("\n", lines);	
      }
    ]]>
	</msxsl:script>
	<xsl:template match="/">
		<xsl:value-of select="user:StripLines(/updates/version)" />
	</xsl:template>
</xsl:stylesheet>
