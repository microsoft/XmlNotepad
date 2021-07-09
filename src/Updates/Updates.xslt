<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" indent="yes"/>
  <xsl:template match="/">
    <html>
      <head>
        <title>
          <xsl:value-of select="updates/application/title"/>
        </title>
        <style>
          body, th, td { font-family: Verdana; font-size:x-small;}
        </style>
      </head>
      <body>
        <center>
          <table width="600"  border="0" cellpadding="0" cellspacing="0">
            <tr>
              <td align="center" bgcolor="teal" style="height: 29px">
                <span style="color:white;background:teal;font-size:18pt">
                  <xsl:value-of select="updates/application/title"/> - Change History
                </span>
              </td>
            </tr>
            <tr>
              <td>
                <br/>
                <p>
                </p>
              </td>
            </tr>
            <tr>
              <td>
                <table border="1" cellpadding="2" >
                  <xsl:apply-templates/>
                </table>
              </td>
            </tr>
          </table>
        </center>
      </body>
    </html>
  </xsl:template>
  <xsl:template match="updates">
    <xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="application">
  </xsl:template>
  <xsl:template match="version">
    <xsl:variable name="color">background:teal;color:white</xsl:variable>
    <tr>
      <td style="{$color}"> </td>
      <td style="{$color}"  align="center">
        Version <xsl:value-of select="@number"/>
      </td>
    </tr>
    <xsl:apply-templates/>
  </xsl:template>
  <xsl:template match="feature">
    <xsl:variable name="color">background:MediumTurquoise</xsl:variable>
    <tr>
      <td style="{$color}" valign="top">Feature</td>
      <td>
        <xsl:value-of select="."/>
      </td>
    </tr>
  </xsl:template>
  <xsl:template match="bug">
    <xsl:variable name="color">background:LightPink</xsl:variable>
    <tr>
      <td style="{$color}" valign="top">Bug</td>
      <td>
        <xsl:value-of select="."/>
      </td>
    </tr>
  </xsl:template>
</xsl:stylesheet>
