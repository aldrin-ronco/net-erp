﻿#pragma checksum "..\..\..\..\..\..\Books\AccountingAccounts\Views\AccountPlanDetailView.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "02040E753B82FE8901808B1B5DD616D4E0E529D4"
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
using NetErp.Books.AccountingAccounts.ViewModels;
using NetErp.Helpers;
using NetErp.IoContainer;
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


namespace NetErp.Books.AccountingAccounts.Views {
    
    
    /// <summary>
    /// AccountPlanDetailView
    /// </summary>
    public partial class AccountPlanDetailView : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 16 "..\..\..\..\..\..\Books\AccountingAccounts\Views\AccountPlanDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal NetErp.Books.AccountingAccounts.Views.AccountPlanDetailView ucAccountDetail;
        
        #line default
        #line hidden
        
        
        #line 29 "..\..\..\..\..\..\Books\AccountingAccounts\Views\AccountPlanDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal BusyIndicator.BusyMask BusyIndicator;
        
        #line default
        #line hidden
        
        
        #line 97 "..\..\..\..\..\..\Books\AccountingAccounts\Views\AccountPlanDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid GridDetails;
        
        #line default
        #line hidden
        
        
        #line 224 "..\..\..\..\..\..\Books\AccountingAccounts\Views\AccountPlanDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox Lv5Code;
        
        #line default
        #line hidden
        
        
        #line 251 "..\..\..\..\..\..\Books\AccountingAccounts\Views\AccountPlanDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox Lv5Name;
        
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
            System.Uri resourceLocater = new System.Uri("/NetErp;component/books/accountingaccounts/views/accountplandetailview.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\..\..\Books\AccountingAccounts\Views\AccountPlanDetailView.xaml"
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
            this.ucAccountDetail = ((NetErp.Books.AccountingAccounts.Views.AccountPlanDetailView)(target));
            return;
            case 2:
            this.BusyIndicator = ((BusyIndicator.BusyMask)(target));
            return;
            case 3:
            
            #line 43 "..\..\..\..\..\..\Books\AccountingAccounts\Views\AccountPlanDetailView.xaml"
            ((System.Windows.Controls.ToolBar)(target)).Loaded += new System.Windows.RoutedEventHandler(this.ToolBar_Loaded);
            
            #line default
            #line hidden
            return;
            case 4:
            this.GridDetails = ((System.Windows.Controls.Grid)(target));
            return;
            case 5:
            this.Lv5Code = ((System.Windows.Controls.TextBox)(target));
            return;
            case 6:
            this.Lv5Name = ((System.Windows.Controls.TextBox)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

