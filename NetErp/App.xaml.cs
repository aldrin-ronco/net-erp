using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Initialize SQLite
            SQLitePCL.Batteries.Init();
            
            PreloadDevExpressAssemblies();
            base.OnStartup(e);
        }

        private static void PreloadDevExpressAssemblies()
        {
            // DevExpress controls
            _ = new DevExpress.Xpf.Grid.GridControl();
            _ = new DevExpress.Xpf.Editors.TextEdit();
            _ = new DevExpress.Xpf.Ribbon.RibbonControl();

            // Force load critical assemblies that cause delays
            _ = System.Reflection.Assembly.LoadFrom(@"GraphQL.Client.Serializer.Newtonsoft.dll");
            _ = System.Reflection.Assembly.LoadFrom(@"GraphQL.Client.Abstractions.dll");
            _ = System.Reflection.Assembly.LoadFrom(@"GraphQL.Primitives.dll");
            _ = System.Reflection.Assembly.LoadFrom(@"GraphQL.Client.Abstractions.Websocket.dll");

            // Load DevExpress Images assembly dynamically (version-independent)
            var devExpressImagesDll = System.IO.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "DevExpress.Images.v*.dll").FirstOrDefault();
            if (devExpressImagesDll != null)
            {
                _ = System.Reflection.Assembly.LoadFrom(devExpressImagesDll);
            }

            // Trigger System.Drawing and Microsoft.CSharp loading
            System.Drawing.Color.FromArgb(255, 0, 0);
            dynamic dummy = new System.Dynamic.ExpandoObject();
        }
    }
}
