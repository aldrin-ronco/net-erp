﻿#pragma checksum "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "02898997815D0931EC34E97D63D9D9DE7826CA51"
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
using CurrencyTextBoxControl;
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
using DevExpress.Xpf.Ribbon;
using DotNetKit.Windows.Controls;
using NetErp.Books.AccountingEntries.Views;
using NetErp.Helpers;
using NetErp.UserControls;
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


namespace NetErp.Books.AccountingEntries.Views {
    
    
    /// <summary>
    /// AccountingEntriesDetailView
    /// </summary>
    public partial class AccountingEntriesDetailView : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 171 "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox SelectedAccountingBookId;
        
        #line default
        #line hidden
        
        
        #line 301 "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal DotNetKit.Windows.Controls.AutoCompleteComboBox AutoCompleteComboBox;
        
        #line default
        #line hidden
        
        
        #line 323 "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox SelectedAccountingEntityOnEntryId;
        
        #line default
        #line hidden
        
        
        #line 360 "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Xceed.Wpf.Toolkit.WatermarkTextBox FilterSearchAccountingEntity;
        
        #line default
        #line hidden
        
        
        #line 366 "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button SearchForAccountingEntityMatch;
        
        #line default
        #line hidden
        
        
        #line 443 "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button AddRecord;
        
        #line default
        #line hidden
        
        
        #line 460 "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal BusyIndicator.BusyMask BusyIndicator;
        
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
            System.Uri resourceLocater = new System.Uri("/NetErp;V1.0.0.0;component/books/accountingentries/views/accountingentriesdetailv" +
                    "iew.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml"
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
            this.SelectedAccountingBookId = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 2:
            this.AutoCompleteComboBox = ((DotNetKit.Windows.Controls.AutoCompleteComboBox)(target));
            return;
            case 3:
            this.SelectedAccountingEntityOnEntryId = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 4:
            this.FilterSearchAccountingEntity = ((Xceed.Wpf.Toolkit.WatermarkTextBox)(target));
            return;
            case 5:
            this.SearchForAccountingEntityMatch = ((System.Windows.Controls.Button)(target));
            return;
            case 6:
            this.AddRecord = ((System.Windows.Controls.Button)(target));
            
            #line 441 "..\..\..\..\..\..\Books\AccountingEntries\Views\AccountingEntriesDetailView.xaml"
            this.AddRecord.Click += new System.Windows.RoutedEventHandler(this.SetFocusOnAutoCompleteComboBox);
            
            #line default
            #line hidden
            return;
            case 7:
            this.BusyIndicator = ((BusyIndicator.BusyMask)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

