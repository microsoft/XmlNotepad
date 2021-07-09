<!-- XSL designed to provide HTML output from Jon Bosak's Shakespeare XML collection -->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:template match="/">
    <html>
      <head>
        <title>
          <xsl:value-of select="PLAY/TITLE" />
        </title>
        <style>
          body, td, th { font-family:Verdana, Arial, helvetica, sans-serif; font-size:x-small; }
        </style>
      </head>
      <body STYLE=" background-color:#EEEEEE">
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
    <DIV STYLE="background-color:teal; color:white; padding:4px">
      <SPAN STYLE="font-weight:bold; color:white">
        <xsl:value-of select="TITLE" />: <xsl:value-of select="TITLE" /></SPAN>
    </DIV>
    <xsl:apply-templates select="SCENE" />
  </xsl:template>
  <!-- Displays list of play's characters -->
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
  <!-- Scope's scenes within the play -->
  <xsl:template match="SCENE">
    <table>
      <tr>
        <td width="15">
        </td>
        <td width="100%">
          <!-- The below code is somewhat funky.  Basically, the XML data is authored
						     somewhat weird.  This is a work around that allows me to "go up and down"
						     a different parent subtree
						-->
          <!-- Handle inner speeches and stage directions -->
          <xsl:apply-templates />
        </td>
      </tr>
    </table>
    <br />
  </xsl:template>
  <!-- Display's an actor's lines -->
  <xsl:template match="SPEECH">
    <br />
    <!-- Note: I'm using the below table for tabbing purposes -->
    <table>
      <tr>
        <!-- List speaker's name (in bold) -->
        <td valign="top" width="150">
          <span STYLE="font-weight:bold; color:black">
            <xsl:value-of select="SPEAKER" />:
          </span>
        </td>
        <!-- The XML data is a little weird in that they break out individual lines of
					     a character's speech, although the line tags *don't* correspond to pauses
					     in the actor's speech.  With the XSL below I'm basically merging the
					     lines into a single text block.
					-->
        <td valign="top" width="400">
          <xsl:for-each select="LINE">
            <span STYLE="color:black">
              <xsl:value-of select="." />
            </span>
          </xsl:for-each>
        </td>
      </tr>
    </table>
  </xsl:template>
  <!-- Display's stage direction (ie: Hamlet enters from right...) -->
  <xsl:template match="STAGEDIR">
    <br />
    <table>
      <tr>
        <td width="600">
          <span STYLE="font-style:italic; color:black">
            Stage Direction: <xsl:value-of select="." /></span>
        </td>
      </tr>
    </table>
    <br />
  </xsl:template>
  <xsl:template match="SCNDESCR">
  </xsl:template>
  <xsl:template match="PLAYSUBT">
  </xsl:template>
</xsl:stylesheet>
