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
    pre {margin:0px;display:inline}]]>
        </STYLE>
        <SCRIPT>
          <x:comment>
            <![CDATA[
  function f(e){
    if (e.className=="ci"){
      if (e.children(0).innerText.indexOf("\n")>0) fix(e,"cb");
    }
    if (e.className=="di"){
      if (e.children(0).innerText.indexOf("\n")>0) fix(e,"db");
    }
    e.id="";
  }
  function fix(e,cl){
    e.className=cl;
    e.style.display="block";
    j=e.parentElement.children(0);
    j.className="c";
    k=j.children(0);
    k.style.visibility="visible";
    k.href="#";
  }
  function ch(e){
    mark=e.children(0).children(0);
    if (mark.innerText=="+"){
      mark.innerText="-";
      for (var i=1;i<e.children.length;i++) e.children(i).style.display="block";
    } else if (mark.innerText=="-"){
      mark.innerText="+";
      for (var i=1;i<e.children.length;i++) e.children(i).style.display="none";
    }
  }
  function ch2(e){
    mark=e.children(0).children(0);
    contents=e.children(1);
    if (mark.innerText=="+"){
      mark.innerText="-";
      if (contents.className=="db"||contents.className=="cb") contents.style.display="block";
      else contents.style.display="inline";
    } else if (mark.innerText=="-"){
      mark.innerText="+";
      contents.style.display="none";
    }
  }
  function cl(){
    e=window.event.srcElement;
    if (e.className!="c"){
      e=e.parentElement;
      if (e.className!="c"){return;}
    }
    e=e.parentElement;
    if (e.className=="e") ch(e);
    if (e.className=="k") ch2(e);
  }
  function ex(){}
  function h(){window.status=" ";}
  document.onclick=cl;
  ]]>
          </x:comment>
        </SCRIPT>
      </HEAD>
      <BODY class="st">
        <div style="padding:5px;" class="side tx">
          
            Your XML document contains no xml-stylesheet processing instruction. To provide
            an XSLT transform, add the following to the top of your file and edit the href
            attribute accordingly:
            
            <pre>
              <span class="m">&lt;?</span><span class="pi">xml-stylesheet</span>&#160;<span class="at">type</span><span class="m">="</span><span class="av">text/xsl</span><span class="m">"</span> <span class="at">href</span><span class="m">="</span><span class="av">stylesheet.xsl</span><span class="m">" ?&gt;</span>
            </pre>
          
          <p>
            You can also enter the XSLT file name using the above "XSLT Location:" text box, but this will not
            persist with your XML document.
          </p>
          <p>
            You can specify a default output file name using the following in your XML documents:
            <pre>
              <span class="m">&lt;?</span><span class="pi">xsl-output</span>&#160;<span class="at">default</span><span class="m">="</span><span class="av">xslt_output</span><span class="m">" ?&gt;</span>
            </pre>
          </p>
        </div>
        <x:apply-templates />
      </BODY>
    </HTML>
  </x:template>
  <x:template match="processing-instruction()">
    <DIV class="e">      
      <SPAN class="m">&lt;?</SPAN>
      <SPAN class="pi">
        <x:value-of select="name()" />&#160;
        <x:value-of select="."/>
      </SPAN>
      <SPAN class="m">?></SPAN>
    </DIV>
  </x:template>
  <x:template match="processing-instruction('xml')">
    <DIV class="e">
      <SPAN class="b">
        <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
      </SPAN>
      <SPAN class="m">&lt;?</SPAN>
      <SPAN class="t">
        xml
        <x:for-each select="@*">
          <SPAN class="at"><x:value-of select="name()" /></SPAN>
          ="
          <SPAN class="av"><x:value-of select="."/></SPAN>
          "
        </x:for-each>
      </SPAN>
      <SPAN class="m">?></SPAN>
    </DIV>
  </x:template>
  <x:template match="@*" xml:space="preserve"> <SPAN><x:attribute name="class"><x:if test="x:*/@*">x</x:if>at</x:attribute><x:value-of select="name()" /></SPAN><SPAN class="m">="</SPAN><SPAN class="av"><x:value-of select="."/></SPAN><SPAN class="m">"</SPAN></x:template>

  <x:template match="comment()">
    <DIV class="k">
      <SPAN>
        <A class="b" onclick="return false" onfocus="h()" STYLE="visibility:hidden"></A>
        <SPAN class="m">&lt;!--</SPAN>
      </SPAN>
      <SPAN id="clean" class="ci">
        <PRE>
          <x:value-of select="."/>
        </PRE>
      </SPAN>
      <SPAN class="b">
        <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
      </SPAN>
      <SPAN class="m">--></SPAN>
      <SCRIPT>f(clean);</SCRIPT>
    </DIV>
  </x:template>
  <x:template match="*">
    <DIV class="e">
      <DIV STYLE="margin-left:1em;text-indent:-2em">
        <SPAN class="b">
          <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
        </SPAN>
        <SPAN class="m">&lt;</SPAN>
        <SPAN class="t">
          <x:value-of select="name()" />
        </SPAN>
        <x:apply-templates select="@*" />
        <SPAN class="m">/></SPAN>
      </DIV>
    </DIV>
  </x:template>
  <x:template match="*[node()]">
    <DIV class="e">
      <DIV class="c">
        <A href="#" onclick="return false" onfocus="h()" class="b"></A>
        <SPAN class="m">&lt;</SPAN>
        <SPAN>
          <x:attribute name="class">
            <x:if test="x:*">x</x:if>
            t
          </x:attribute>
          <x:value-of select="name()" />
        </SPAN>
        <x:apply-templates select="@*" />
        <SPAN class="m">></SPAN>
      </DIV>
      <DIV>
        <x:apply-templates />
        <DIV>
          <SPAN class="b">
            <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
          </SPAN>
          <SPAN class="m">&lt;/</SPAN>
          <SPAN>
            <x:attribute name="class">
              <x:if test="x:*">x</x:if>
              t
            </x:attribute>
            <x:value-of select="name()" />
          </SPAN>
          <SPAN class="m">></SPAN>
        </DIV>
      </DIV>
    </DIV>
  </x:template>
  <x:template match="*[text() and not(comment() or processing-instruction())]">
    <!--$or$cdata()-->
    <DIV class="e">
      <DIV STYLE="margin-left:1em;text-indent:-2em">
        <SPAN class="b">
          <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
        </SPAN>
        <SPAN class="m">&lt;</SPAN>
        <SPAN>
          <x:attribute name="class">
            <x:if test="x:*">x</x:if>
            t
          </x:attribute>
          <x:value-of select="name()" />
        </SPAN>
        <x:apply-templates select="@*" />
        <SPAN class="m">></SPAN>
        <SPAN class="tx">
          <x:value-of select="."/>
        </SPAN>
        <SPAN class="m">&lt;/</SPAN>
        <SPAN>
          <x:attribute name="class">
            <x:if test="x:*">x</x:if>
            t
          </x:attribute>
          <x:value-of select="name()" />
        </SPAN>
        <SPAN class="m">></SPAN>
      </DIV>
    </DIV>
  </x:template>
  <x:template match="*[*]">
    <DIV class="e">
      <DIV class="c" STYLE="margin-left:1em;text-indent:-2em">
        <A href="#" onclick="return false" onfocus="h()" class="b"></A>
        <SPAN class="m">&lt;</SPAN>
        <SPAN>
          <x:attribute name="class">
            <x:if test="x:*">x</x:if>
            t
          </x:attribute>
          <x:value-of select="name()" />
        </SPAN>
        <x:apply-templates select="@*" />
        <SPAN class="m">></SPAN>
      </DIV>
      <DIV>
        <x:apply-templates />
        <DIV>
          <SPAN class="b">
            <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
          </SPAN>
          <SPAN class="m">&lt;/</SPAN>
          <SPAN>
            <x:attribute name="class">
              <x:if test="x:*">x</x:if>
              t
            </x:attribute>
            <x:value-of select="name()" />
          </SPAN>
          <SPAN class="m">></SPAN>
        </DIV>
      </DIV>
    </DIV>
  </x:template>
</x:stylesheet>
