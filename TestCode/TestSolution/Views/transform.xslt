<?xml version="1.0" encoding="windows-utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:req="urn:request-info">
  <xsl:output method="html" indent="no" encoding="utf-8" />

  <xsl:variable name="URL">
    <xsl:choose>
      <xsl:when test="req:getParam('url')">
        <xsl:value-of select="req:getParam('url'), '&amp;')" />
      </xsl:when>
      <xsl:otherwise></xsl:otherwise>
    </xsl:choose>
  </xsl:variable>
  
  <xsl:template match="/">
    <head>
      <title>
        <xsl:value-of select="Root/@TITLE" />
      </title>
      <link rel="stylesheet" type="text/css" href="Content/{@site}.css" />
      <script src="Scripts/{@script}.js" type="text/javascript" language="JavaScript" />
    </head>
    <body>
      <xsl:for-each select="/Error" >
        <span class="error">
          <b>Error</b>: <xsl:value-of select="@message" />
        </span>
      </xsl:for-each>

      <xsl:copy-of select="Content" />
    </body>
  </xsl:template>
</xsl:stylesheet>