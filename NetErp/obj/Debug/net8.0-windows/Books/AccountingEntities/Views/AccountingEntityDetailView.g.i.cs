﻿#pragma checksum "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "58F9EE3AB4A192D0C030B57465D7022DF151DB8F"
//------------------------------------------------------------------------------
// <auto-generated>
//     Este código fue generado por una herramienta.
//     Versión de runtime:4.0.30319.42000
//
//     Los cambios en este archivo podrían causar un comportamiento incorrecto y se perderán si
//     se vuelve a generar el código.
// </auto-generated>
//------------------------------------------------------------------------------

using BusyIndicator;
using Caliburn.Micro;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Mvvm.UI.ModuleInjection;
using DevExpress.Xpf.DXBinding;
using NetErp.Books.AccountingEntities.Views;
using NetErp.Helpers;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Chromes;
using Xceed.Wpf.Toolkit.Converters;
using Xceed.Wpf.Toolkit.Core;
using Xceed.Wpf.Toolkit.Core.Converters;
using Xceed.Wpf.Toolkit.Core.Input;
using Xceed.Wpf.Toolkit.Core.Media;
using Xceed.Wpf.Toolkit.Core.Utilities;
using Xceed.Wpf.Toolkit.Mag.Converters;
using Xceed.Wpf.Toolkit.Panels;
using Xceed.Wpf.Toolkit.Primitives;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit.PropertyGrid.Commands;
using Xceed.Wpf.Toolkit.PropertyGrid.Converters;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Xceed.Wpf.Toolkit.Zoombox;


namespace NetErp.Books.AccountingEntities.Views {
    
    
    /// <summary>
    /// AccountingEntityDetailView
    /// </summary>
    public partial class AccountingEntityDetailView : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 40 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal BusyIndicator.BusyMask BusyIndicator;
        
        #line default
        #line hidden
        
        
        #line 63 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Cancel;
        
        #line default
        #line hidden
        
        
        #line 75 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Save;
        
        #line default
        #line hidden
        
        
        #line 91 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid GridSource;
        
        #line default
        #line hidden
        
        
        #line 118 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox RegimeDictionary;
        
        #line default
        #line hidden
        
        
        #line 138 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox IdentificationTypes;
        
        #line default
        #line hidden
        
        
        #line 158 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox IdentificationNumber;
        
        #line default
        #line hidden
        
        
        #line 228 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox BusinessName;
        
        #line default
        #line hidden
        
        
        #line 243 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox FirstName;
        
        #line default
        #line hidden
        
        
        #line 257 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox MiddleName;
        
        #line default
        #line hidden
        
        
        #line 267 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox FirstLastName;
        
        #line default
        #line hidden
        
        
        #line 277 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox MiddleLastName;
        
        #line default
        #line hidden
        
        
        #line 285 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox Phone1;
        
        #line default
        #line hidden
        
        
        #line 295 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox Phone2;
        
        #line default
        #line hidden
        
        
        #line 302 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox CellPhone1;
        
        #line default
        #line hidden
        
        
        #line 309 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox CellPhone2;
        
        #line default
        #line hidden
        
        
        #line 318 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox Countries;
        
        #line default
        #line hidden
        
        
        #line 333 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox SelectedCountry_Departments;
        
        #line default
        #line hidden
        
        
        #line 347 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox SelectedDepartment_Cities;
        
        #line default
        #line hidden
        
        
        #line 353 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox Address;
        
        #line default
        #line hidden
        
        
        #line 384 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox EmailDescription;
        
        #line default
        #line hidden
        
        
        #line 390 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button AddEmail;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/NetErp;component/books/accountingentities/views/accountingentitydetailview.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.2.0")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal System.Delegate _CreateDelegate(System.Type delegateType, string handler) {
            return System.Delegate.CreateDelegate(delegateType, this, handler);
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.2.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.BusyIndicator = ((BusyIndicator.BusyMask)(target));
            return;
            case 2:
            
            #line 62 "..\..\..\..\..\..\Books\AccountingEntities\Views\AccountingEntityDetailView.xaml"
            ((System.Windows.Controls.ToolBar)(target)).Loaded += new System.Windows.RoutedEventHandler(this.ToolBar_Loaded);
            
            #line default
            #line hidden
            return;
            case 3:
            this.Cancel = ((System.Windows.Controls.Button)(target));
            return;
            case 4:
            this.Save = ((System.Windows.Controls.Button)(target));
            return;
            case 5:
            this.GridSource = ((System.Windows.Controls.Grid)(target));
            return;
            case 6:
            this.RegimeDictionary = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 7:
            this.IdentificationTypes = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 8:
            this.IdentificationNumber = ((System.Windows.Controls.TextBox)(target));
            return;
            case 9:
            this.BusinessName = ((System.Windows.Controls.TextBox)(target));
            return;
            case 10:
            this.FirstName = ((System.Windows.Controls.TextBox)(target));
            return;
            case 11:
            this.MiddleName = ((System.Windows.Controls.TextBox)(target));
            return;
            case 12:
            this.FirstLastName = ((System.Windows.Controls.TextBox)(target));
            return;
            case 13:
            this.MiddleLastName = ((System.Windows.Controls.TextBox)(target));
            return;
            case 14:
            this.Phone1 = ((System.Windows.Controls.TextBox)(target));
            return;
            case 15:
            this.Phone2 = ((System.Windows.Controls.TextBox)(target));
            return;
            case 16:
            this.CellPhone1 = ((System.Windows.Controls.TextBox)(target));
            return;
            case 17:
            this.CellPhone2 = ((System.Windows.Controls.TextBox)(target));
            return;
            case 18:
            this.Countries = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 19:
            this.SelectedCountry_Departments = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 20:
            this.SelectedDepartment_Cities = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 21:
            this.Address = ((System.Windows.Controls.TextBox)(target));
            return;
            case 22:
            this.EmailDescription = ((System.Windows.Controls.TextBox)(target));
            return;
            case 23:
            this.AddEmail = ((System.Windows.Controls.Button)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

