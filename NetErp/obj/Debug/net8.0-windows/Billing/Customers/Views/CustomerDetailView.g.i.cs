﻿#pragma checksum "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "88CBA08BA0BAD89932B2FB92640860C918031163"
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
using Common.Config;
using DevExpress.Core;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Mvvm.UI.ModuleInjection;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.ConditionalFormatting;
using DevExpress.Xpf.Core.DataSources;
using DevExpress.Xpf.Core.Serialization;
using DevExpress.Xpf.Core.ServerMode;
using DevExpress.Xpf.DXBinding;
using DevExpress.Xpf.Data;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.DataPager;
using DevExpress.Xpf.Editors.DateNavigator;
using DevExpress.Xpf.Editors.ExpressionEditor;
using DevExpress.Xpf.Editors.Filtering;
using DevExpress.Xpf.Editors.Flyout;
using DevExpress.Xpf.Editors.Popups;
using DevExpress.Xpf.Editors.Popups.Calendar;
using DevExpress.Xpf.Editors.RangeControl;
using DevExpress.Xpf.Editors.Settings;
using DevExpress.Xpf.Editors.Settings.Extension;
using DevExpress.Xpf.Editors.Validation;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.ConditionalFormatting;
using DevExpress.Xpf.Grid.LookUp;
using DevExpress.Xpf.Grid.TreeList;
using DevExpress.Xpf.Ribbon;
using NetErp.Billing.Customers.Views;
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


namespace NetErp.Billing.Customers.Views {
    
    
    /// <summary>
    /// CustomerDetailView
    /// </summary>
    public partial class CustomerDetailView : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 44 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal BusyIndicator.BusyMask BusyIndicator;
        
        #line default
        #line hidden
        
        
        #line 73 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Bars.BarButtonItem btnBack;
        
        #line default
        #line hidden
        
        
        #line 100 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid GridSource;
        
        #line default
        #line hidden
        
        
        #line 127 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox RegimeDictionary;
        
        #line default
        #line hidden
        
        
        #line 147 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox IdentificationTypes;
        
        #line default
        #line hidden
        
        
        #line 160 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.SpinEdit IdentificationNumber;
        
        #line default
        #line hidden
        
        
        #line 244 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit BusinessName;
        
        #line default
        #line hidden
        
        
        #line 257 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit FirstName;
        
        #line default
        #line hidden
        
        
        #line 270 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit MiddleName;
        
        #line default
        #line hidden
        
        
        #line 282 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit FirstLastName;
        
        #line default
        #line hidden
        
        
        #line 294 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit MiddleLastName;
        
        #line default
        #line hidden
        
        
        #line 306 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit Phone1;
        
        #line default
        #line hidden
        
        
        #line 321 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit Phone2;
        
        #line default
        #line hidden
        
        
        #line 335 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit CellPhone1;
        
        #line default
        #line hidden
        
        
        #line 349 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit CellPhone2;
        
        #line default
        #line hidden
        
        
        #line 477 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DevExpress.Xpf.Editors.TextEdit Address;
        
        #line default
        #line hidden
        
        
        #line 535 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox EmailDescription;
        
        #line default
        #line hidden
        
        
        #line 544 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button AddEmail;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.8.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/NetErp;V1.0.0.0;component/billing/customers/views/customerdetailview.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\..\..\Billing\Customers\Views\CustomerDetailView.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.8.0")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal System.Delegate _CreateDelegate(System.Type delegateType, string handler) {
            return System.Delegate.CreateDelegate(delegateType, this, handler);
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "8.0.8.0")]
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
            this.btnBack = ((DevExpress.Xpf.Bars.BarButtonItem)(target));
            return;
            case 3:
            this.GridSource = ((System.Windows.Controls.Grid)(target));
            return;
            case 4:
            this.RegimeDictionary = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 5:
            this.IdentificationTypes = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 6:
            this.IdentificationNumber = ((DevExpress.Xpf.Editors.SpinEdit)(target));
            return;
            case 7:
            this.BusinessName = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 8:
            this.FirstName = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 9:
            this.MiddleName = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 10:
            this.FirstLastName = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 11:
            this.MiddleLastName = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 12:
            this.Phone1 = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 13:
            this.Phone2 = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 14:
            this.CellPhone1 = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 15:
            this.CellPhone2 = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 16:
            this.Address = ((DevExpress.Xpf.Editors.TextEdit)(target));
            return;
            case 17:
            this.EmailDescription = ((System.Windows.Controls.TextBox)(target));
            return;
            case 18:
            this.AddEmail = ((System.Windows.Controls.Button)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

