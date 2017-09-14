<%@ Page Language="C#" AutoEventWireup="false" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title></title>
</head>
<body>
    <% 
        System.Reflection.Assembly asm = null;
        try
        {
            asm = System.Reflection.Assembly.LoadFrom(
                  System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath
                + @"\bin\Microsoft.SourceBrowser.SourceIndexServer.dll");
        }
        catch (System.Exception ex) { Response.Write(ex.Message); }
        
        var obj = asm.CreateInstance("Source.EngineDebug");
        var m = obj.GetType().GetMethod("Write");
        m.Invoke(obj, null);
    %>
</body>
</html>
