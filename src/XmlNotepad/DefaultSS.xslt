<?xml version="1.0" ?>
<x:stylesheet xmlns:x="http://www.w3.org/1999/XSL/Transform" version="1.0"
              xmlns:dt="http://www.w3.org/2001/XMLSchema-instance"
              xmlns:d2="urn:schemas-microsoft-com:datatypes">
  <x:output method="html"/>
  <x:template match="/">
    <HTML>
      <HEAD>
        <STYLE>
          <![CDATA[
    body {font-family:$FONT_FAMILY;font-size:$FONT_SIZE;margin-right:1.5em;background:$BACKGROUND_COLOR}
    .c{ }
    .m{color:$MARKUP_COLOR}                   /* markup */
    .b{color:red;font-weight:bold;text-decoration:none}  /* non breaking space */
    .e{margin-left:1em;text-indent:-1em;margin-right:1em}  /* expandable */
    .k{margin-left:1em;text-indent:-1em;margin-right:1em}  /* outer comment (non-expandable)? */
    .at{color:$ATTRIBUTE_NAME_COLOR}          /* attribute name */
    .av {color:$ATTRIBUTE_VALUE_COLOR;}       /* attribute value */
    .t {color:$ELEMENT_COLOR}                 /* element name */
    .tx{color:$TEXT_COLOR}   /* text content */
    .pi{color:$PI_COLOR}                      /* pi name and content */
    .ci{color:$COMMENT_COLOR}                 /* comment value */    
    .side{background-color:$SIDENOTE_COLOR}   /* intro div */    
    .outputtip { display:$OUTPUT_TIP_DISPLAY }
    a { color:$ELEMENT_COLOR }
    a:visited { color:$PI_COLOR }
    a:hover { color:$PI_COLOR }
    pre {margin:0px;display:inline}]]>
        </STYLE>
      </HEAD>
      <BODY class="st">
        <div style="padding:5px;" class="side tx">
          
            Your XML document contains no xml-stylesheet processing instruction. To provide
            an <a href="https://www.tutorialspoint.com/xslt/index.htm" target="_new">XSLT transform</a>, add the following to the top of your file and edit the href
            attribute accordingly:

            <pre>
              <span class="m">&lt;?</span><span class="pi">xml-stylesheet</span>&#160;<span class="at">type</span><span class="m">="</span><span class="av">text/xsl</span><span class="m">" </span><span class="at">href</span><span class="m">="</span><span class="av">stylesheet.xsl</span><span class="m">" ?&gt;</span>
            </pre>
          
          <p>
            You can also enter the XSLT file name using the above "XSLT Location:" text box, but this will not
            persist with your XML document.
          </p>
          <div class="outputtip">
            You can specify a default output file name using the following in your XML documents:
            <pre>
              <span class="m">&lt;?</span><span class="pi">xsl-output</span>&#160;<span class="at">default</span><span class="m">="</span><span class="av">xslt_output</span><span class="m">" ?&gt;</span>
            </pre>
          </div>
        </div>
        <x:apply-templates />
      </BODY>
    </HTML>
  </x:template>
  <!-- try and pretty print xml declaration attributes, if xslt will match it -->
  <x:template match="processing-instruction('xml')">
    <div class="e">
      <span class="m">&lt;?</span>
      <span class="t">
        xml
        <x:for-each select="@*">
          <span class="at"><x:value-of select="name()" /></span>
          ="
          <span class="av"><x:value-of select="."/></span>
          "
        </x:for-each>
      </span>
      <span class="m">?></span>
    </div>
  </x:template>
  <!-- all other processing instructions -->
  <x:template match="processing-instruction()">
    <div class="e">
      <span class="m">&lt;?</span>
      <span class="pi">
        <x:value-of select="name()" />&#160;
        <x:value-of select="."/>
      </span>
      <span class="m">?></span>
    </div>
  </x:template>
  <!-- attributes -->
  <x:template match="@*" xml:space="preserve"> 
    <span><x:attribute name="class"><x:if test="x:*/@*">x</x:if>at</x:attribute><x:value-of select="name()" /></span><span class="m">="</span><span class="av"><x:value-of select="."/></span><span class="m">"</span>
  </x:template>

  <!-- comments -->
  <x:template match="comment()">
    <div class="k">
      <span>        
        <span class="m">&lt;!--</span>
      </span>
      <span id="clean" class="ci">
        <pre><x:value-of select="."/></pre>
      </span>
      <span class="b">
        <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
      </span>
      <span class="m">--></span>     
    </div>
  </x:template>
  <!-- leaf nodes -->
  <x:template match="*">
    <div class="e">
      <div STYLE="margin-left:1em;text-indent:-2em">
        <span class="b">
          <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
        </span>
        <span class="m">&lt;</span>
        <span class="t">
          <x:value-of select="name()" />
        </span>
        <x:apply-templates select="@*" />
        <span class="m">/></span>
      </div>
    </div>
  </x:template>
  <!-- nodes containing text only -->
  <x:template match="*[text() and not(comment() or processing-instruction())]">    
    <div class="e">
      <div STYLE="margin-left:1em;text-indent:-2em">
        <span class="b">
          <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
        </span>
        <span class="m">&lt;</span>
        <span class="t">
          <x:value-of select="name()" />
        </span>
        <x:apply-templates select="@*" />
        <span class="m">></span>
        <span class="tx">
          <x:value-of select="."/>
        </span>
        <span class="m">&lt;/</span>
        <span class="t">
          <x:value-of select="name()" />
        </span>
        <span class="m">></span>
      </div>
    </div>
  </x:template>
  <!-- nodes containing children -->
  <x:template match="*[*]">
    <div class="e">      
      <div class="c" STYLE="margin-left:1em;text-indent:-2em">
        <A href="#" onclick="return false" onfocus="h()" class="b"></A>
        <span class="m">&lt;</span>
        <span class="t">
          <x:value-of select="name()" />
        </span>
        <x:apply-templates select="@*" />
        <span class="m">></span>
      </div>
      <div>
        <x:apply-templates />
        <div>
          <span class="b">
            <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
          </span>
          <span class="m">&lt;/</span>
          <span class="t">
            <x:value-of select="name()" />
          </span>
          <span class="m">></span>
        </div>
      </div>
    </div>
  </x:template>
</x:stylesheet>
