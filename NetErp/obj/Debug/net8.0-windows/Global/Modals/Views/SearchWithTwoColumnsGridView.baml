   M S B A M L   `   `   `  ����A  =NetErp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:NetErp.UserControlsNetErp.UserControls  5
      G(clr-namespace:NetErp.Global.Modals.ViewsNetErp.Global.Modals.Views  5      =    7NetErp.Global.Modals.Views.SearchWithTwoColumnsGridView   5      a ]DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aW SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      ;dx2http://schemas.devexpress.com/winfx/2008/xaml/core  5      a ]DevExpress.Xpf.Grid.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49af	 bDevExpress.Xpf.Grid.v23.2.Core, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49al
 hDevExpress.Xpf.Grid.v23.2.Extensions, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a@dxg2http://schemas.devexpress.com/winfx/2008/xaml/grid  	 
 5      H DBusyIndicator, Version=2.1.0.0, Culture=neutral, PublicKeyToken=nullBbusyIndicator.https://github.com/moh3ngolshani/BusyIndicator  5      ?dxe5http://schemas.devexpress.com/winfx/2008/xaml/editors  5      ] YDevExpress.Mvvm.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aAdxmvvm2http://schemas.devexpress.com/winfx/2008/xaml/mvvm   5      Fmc;http://schemas.openxmlformats.org/markup-compatibility/2006   5	      ,uc!clr-namespace:NetErp.UserControls   5
      <d2http://schemas.microsoft.com/expression/blend/2008   5      6local(clr-namespace:NetErp.Global.Modals.Views   5      .��5      � $	��450��6   $
��1200��6     � RowDefinitions  5   
   �� 5      $
��auto �6   �� 5      $��* �6   �� 5      $
��auto �6   .+�5   
   �� $��0��6   $
��auto��5   
   $
��auto��6   $��5q�6%    �� CharacterCasing$ Upper=�60   ��5      ��  �� Path$ FilterSearch� �� Mode$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=� �� Delay$	 300��.��5   H   2  ,DevExpress.Mvvm.UI.Interactivity.Interaction   	Behaviors 5      &   DevExpress.Mvvm.UI.FocusBehavior  5         PropertyName$ FilterSearchFocus��6@   �  �DevExpress.Mvvm.UI.Interactivity.EventTriggerBase`1[[System.Windows.Controls.Control, PresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]   SourceObject 6'   �� 6@   %  DevExpress.Mvvm.UI.KeyToCommand  5      	   
KeyGesture$	 Enter��6&   +  %DevExpress.Mvvm.UI.EventToCommandBase
   Command
 69   �� *FilterSearchEnterCommand+.��5   ,   5        BusyIndicator.BusyMask  5   
   -   5          Name$ BusyIndicator��5   !      IndicatorType$ BouncingDot=�5          BusyContent$ Please wait...��5!         BusyContentMargin$ 0,20,0,0q�5"         IsBusyAtStartup$ False��5#      $��1��5$         IsBusy 5      �� *IsBusy+$ PropertyChanged=�.��5&      %  DevExpress.Xpf.Grid.GridControl  $��1��6   ) 	 #DevExpress.Xpf.Grid.DataControlBase   AutoGenerateColumns$
 None=�5(      #   EnableSmartColumnsGeneration$
 True��6*   $��5q�6N      ItemsSource 5&   +   �� $ ItemsSource�$ TwoWay=�$ PropertyChanged=�   SelectedItem 5'      �� $ SelectedItem�$ TwoWay=�$ PropertyChanged=�   View 5)      #	  DevExpress.Xpf.Grid.TableView	  5+       	  AllowCascadeUpdate$ False��6(   &
   DevExpress.Xpf.Grid.GridViewBase 
  AllowPaging$ False��5,      & 	  DevExpress.Xpf.Grid.DataViewBase"   EnableSelectedRowAppearance$
 True��5-       
  ShowGroupPanel$ False��5.       	  AlternateRowBackground
 �����   AllowEditing$ False��6R    	  RowDoubleClickCommand 6g   �� *RowDoubleClickCommand+ 50        51      $	 Enter��6*   
 6=   �� *EnterKeyCommand+  52      $		 Esc��6*   
 6;   �� *GridEscKeyCommand+  53      $ 	GridFocus��6D    6+   �� 6D      Columns. 55      $  DevExpress.Xpf.Grid.GridColumn  $ 	 DevExpress.Xpf.Grid.BaseColumn'    HorizontalHeaderContentAlignment$ Center=�6A      Header 6!   �� *FieldHeader1+$ 	 DevExpress.Xpf.Grid.ColumnBase    	FieldName  56      �� *
FieldData1+  57      $ Center=�6A   !   Width2 	 ,DevExpress.Xpf.Grid.GridColumnWidthConverter$! * 6k    6!   �� *FieldHeader2+  58      �� *
FieldData2+59      5:      $   NetErp.UserControls.GridPaging  5;   
   $��2��6   $��0 5 0 5q�6%   $��Stretch=�5<      $��Center=�5=      "   ChangedIndexCommand" 5>      �� *PaginationCommand+#   	PageIndex# 5?      �� *	PageIndex+$ TwoWay=�$ PropertyChanged=�$   PageSize$ 5@      �� *
PageSize+$ TwoWay=�$ PropertyChanged=�%   
TotalCount% 5A      �� *
TotalCount+$ TwoWay=�$ PropertyChanged=�5B      5C      