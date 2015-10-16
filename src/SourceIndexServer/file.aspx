<%@ Page Language="C#" AutoEventWireup="false" %>
<!DOCTYPE html>
<% 
	var index = Microsoft.SourceBrowser.SourceIndexServer.Models.Index.Instance;
	index.File();
%>