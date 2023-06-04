<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:template match="/">
    <html>
      <head>
        <title>
          <xsl:value-of select="PLAY/TITLE" />
        </title>
        <style>
          body, td, th { font-family:Verdana, Arial, helvetica, sans-serif; font-size:x-small; }
          body { background-color:#EEEEEE; }
          h1, h2 { text-align:center; }
          .act-title { background-color:teal; color:white; padding:4px; }
          .speaker { font-weight:bold; color:black; }
          .stage-dir { font-style:italic; color:black; }
        </style>
      </head>
      <body>
        <!-- Display title and author of play -->
        <center>
          <h1>
            <xsl:value-of select="PLAY/TITLE" />
          </h1>
          <p>
            <h2>William Shakespeare</h2>
          </p>
        </center>
        <xsl:apply-templates select="PLAY/*" />
      </body>
    </html>
  </xsl:template>
  
  <xsl:template match="ACT">
    <div class="act-title">
      <span class="act-title">
        <xsl:value-of select="TITLE" />: <xsl:value-of select="TITLE" />
      </span>
    </div>
    <xsl:apply-templates select="SCENE" />
  </xsl:template>
  
  <xsl:template match="PERSONAE">
    <br />
    <br />
    <table>
      <tr>
        <td valign="top">
          <b>Characters: </b>
        </td>
        <td valign="top">
          <xsl:for-each select="PERSONA">
            <xsl:value-of select="." />
            <br />
          </xsl:for-each>
        </td>
      </tr>
    </table>
    <br />
    <br />
  </xsl:template>
  
  <xsl:template match="SCENE">
    <table>
      <tr>
        <td width="15"></td>
        <td width="100%">
          <xsl:apply-templates />
        </td>
      </tr>
    </table>
    <br />
  </xsl:template>
  
  <xsl:template match="SPEECH">
    <br />
    <table>
      <tr>
        <td valign="top" width="150">
          <span class="speaker">
            <xsl:value-of select="SPEAKER" />:
          </span>
        </td>
        <td valign="top" width="400">
          <xsl:for-each select="LINE">
            <span class="speaker">
              <xsl:value-of select="." />
            </span>
          </xsl:for-each>
        </td>
      </tr>
    </table>
  </xsl:template>
  
  <xsl:template match="STAGEDIR">
    <br />
    <table>
      <tr>
        <td width="600">
          <span class="stage-dir">
            Stage Direction: <xsl:value-of select="." />
          </span>
        </td>
      </tr>
    </table>
    <br />
  </xsl:template>
</xsl:stylesheet>
