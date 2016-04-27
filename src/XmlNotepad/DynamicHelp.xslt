<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" 
  xmlns:xs="http://www.w3.org/2001/XMLSchema" 
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html"/>
	
  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="*">
		<xsl:choose>
			<xsl:when test="local-name() = 'tooltip'"><H3>Summary</H3><xsl:apply-templates/></xsl:when>
			<xsl:when test="local-name() = 'remarks'"><H3>Remarks</H3><xsl:apply-templates/></xsl:when>
			<xsl:otherwise>
				<xsl:copy>
					<xsl:apply-templates select="@* | node()" />
				</xsl:copy>
			</xsl:otherwise>
		</xsl:choose>    
  </xsl:template>  
	
  <xsl:template match="/">
    <html>
			<style>
				body { font-size:10pt; font-family:verdana; }
			</style>
      <body>
        <xsl:apply-templates />
      </body>
    </html>
  </xsl:template>
	<xsl:template match="nothing">
		<font color="rgb(43,145,175)">
			Dynamic help displays the xsd:documentation for the selected node.  You 
			currently have no associated XML schema or your selected node has no 
			corresponding xsd:documentation in an xsd:annotation.
		</font>
  </xsl:template>
		<xsl:template match="errors">
		<font color="rgb(43,145,175)">
			Dynamic help displays the xsd:annotations for the selected node but
			this only works when you do not have validation errors.  See the
			Task List for the error information.
		</font>
  </xsl:template>
</xsl:stylesheet>