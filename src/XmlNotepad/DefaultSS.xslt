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
    BODY{font:x-small 'Verdana';margin-right:1.5em} 
    .c{cursor:hand} 
    .b{color:red;font-family:'Courier New';font-weight:bold;text-decoration:none} 
    .e{margin-left:1em;text-indent:-1em;margin-right:1em} 
    .k{margin-left:1em;text-indent:-1em;margin-right:1em} 
    .at{color:red} 
    .xat{color:#990099}
    .t{color:#990000} 
    .xt{color:#990099} 
    .ns{color:red} 
    .dt{color:blue} 
    .m{color:blue} 
    .tx{font-weight:bold} 
    .db{text-indent:0px;margin-left:0;margin-top:-1em;margin-bottom:-1em;padding-left:0;border-left:1px solid #CCCCCC;font:small Courier} 
    .di{font:small Courier} 
    .d{color:blue} 
    .pi{color:blue} 
    .cb{text-indent:0px;margin-left:0;margin-top:-1em;margin-bottom:-1em;padding-left:0;font:small Courier;color:green} 
    .ci{font:small Courier;color:green} 
    .av {color:blue;}
    PRE{margin:0px;display:inline}]]>
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
        <div  style="border:1 dashed navy; padding-left:5px;padding-right:5px;background-color:#FFFFB3">
          <p>
            Your XML document contains no xml-stylesheet processing instruction. To provide
            an XSLT transform, add the following to the top of your file and edit the href
            attribute accordingly:</p>
            <pre style="font-size:small">
              <span class="d">&lt;?</span><span class="t">xml-stylesheet</span>&#160;<span class="at">type</span><span class="d">=</span>"<span class="av">text/xsl</span>" <span class="at">href</span><span class="d">=</span>"<span class="av">stylesheet.xsl</span>"<span class="d">?&gt;</span>
            </pre>
          <p>
            You can also enter the XSLT file name using the above text box, but this will not
            persist with your XML document.
          </p>
          <p>
            The following HTML is provided by the default XSLT transform which is designed
            to pretty print your XML document.
          </p>
        </div>
        <x:apply-templates />
      </BODY>
    </HTML>
  </x:template>
  <x:template match="processing-instruction()">
    <DIV class="e">
      <SPAN class="b">
        <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
      </SPAN>
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
      <SPAN class="pi">
        xml
        <x:for-each select="@*">
          <x:value-of select="name()" />
          ="
          <x:value-of select="."/>
          "
        </x:for-each>
      </SPAN>
      <SPAN class="m">?></SPAN>
    </DIV>
  </x:template>
  <x:template match="@*" xml:space="preserve"> <SPAN><x:attribute name="class"><x:if test="x:*/@*">x</x:if>at</x:attribute><x:value-of select="name()" /></SPAN><SPAN class="m">="</SPAN><SPAN class="av"><x:value-of select="."/></SPAN><SPAN class="m">"</SPAN></x:template>
  <x:template match="*[starts-with(name(),'xml')]">
    <SPAN class="ns">
      <x:value-of select="name()" />
    </SPAN>
    <SPAN class="m">="</SPAN>
    <B class="ns">
      <x:value-of select="."/>
    </B>
    <SPAN class="m">"</SPAN>
  </x:template>
  <x:template match="@dt:*|@d2:*" xml:space="preserve">
    <SPAN class="dt"><x:value-of select="name()" /></SPAN><SPAN class="m">="</SPAN><B class="dt"><x:value-of select="."/></B><SPAN class="m">"</SPAN></x:template>
  <x:template match="text()">
    <x:if test="string-length(normalize-space(.))>0">
      <DIV class="e">
        <SPAN class="b">
          <x:text disable-output-escaping="yes"><![CDATA[&nbsp;]]></x:text>
        </SPAN>
        <SPAN class="tx">
          <x:value-of select="."/>
        </SPAN>
      </DIV>
    </x:if>
  </x:template>
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
        <SPAN>
          <x:attribute name="class">
            <x:if test="x:*">x</x:if>t
          </x:attribute>
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