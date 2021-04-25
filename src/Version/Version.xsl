<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"
                xmlns:msbuild="http://schemas.microsoft.com/developer/msbuild/2003">
  <xsl:output method="text"/>
  <xsl:template match="/">
    <xsl:value-of select="//msbuild:ApplicationVersion" />
  </xsl:template>
</xsl:stylesheet>