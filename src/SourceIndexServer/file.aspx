<%@ Page Language="C#" AutoEventWireup="false" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title></title>
</head>
<body>
    <% 
        var index = Microsoft.SourceBrowser.SourceIndexServer.Models.Index.Instance;
        index.File();
    %>
</body>
</html>
