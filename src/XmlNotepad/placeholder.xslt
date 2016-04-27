<?xml version="1.0" ?>
<x:stylesheet xmlns:x="http://www.w3.org/1999/XSL/Transform" version="1.0"
              xmlns:dt="http://www.w3.org/2001/XMLSchema-instance"
              xmlns:d2="urn:schemas-microsoft-com:datatypes">
  <x:output method="html"/>
  <x:template match="/">
    <HTML>
      <STYLE>
        h1,h2,h3,h4,h5 { color:navy; }
        body, td { font-family: Verdana; font-size:x-small;}
        dt { font-weight: bold; }
        .delim { color:blue;}
        .attr { color:red;}
        .attrvalue { color:blue;}
        .name { color:maroon;}
      </STYLE>
      <BODY>
        <p>
          Your XML document contains no XSLT filename.
          The XSLT transform is defined by a processing instruction at the top of 
          your XML document that looks like this:
          <pre style="font-size:small">
            <span class="delim">&lt;?</span><span class="name">xml-stylesheet</span>&#160;<span class="attr">type</span><span class="delim">=</span>"<span class="attrvalue">text/xsl</span>" <span class="attr">href</span><span class="delim">=</span>"<span class="attrvalue">willy.xsl</span>"<span class="delim">?&gt;</span>
          </pre>          
          You can enter the XSLT file name using the above text box, but if you want
          the XSLT file name to persist with your document you should cut and paste the above
          processing instruction and move it to the top of your document and edit the href
          attribute appropriately.
        </p>
      </BODY>
    </HTML>
  </x:template>
</x:stylesheet>