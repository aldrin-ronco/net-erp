   M S B A M L   `   `   `  ����A  =Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null=+clr-namespace:Common.Config;assembly=CommonCommon.Config  5
      A =NetErp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nulla5clr-namespace:NetErp.Inventory.MeasurementUnits.Views'NetErp.Inventory.MeasurementUnits.Views 5      G   ANetErp.Inventory.MeasurementUnits.Views.MeasurementUnitMasterView   5      a ]DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aW SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      a	 ]DevExpress.Xpf.Grid.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49af
 bDevExpress.Xpf.Grid.v23.2.Core, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49al hDevExpress.Xpf.Grid.v23.2.Extensions, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a@dxg2http://schemas.devexpress.com/winfx/2008/xaml/grid 	 
  5      ] YDevExpress.Mvvm.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aAdxmvvm2http://schemas.devexpress.com/winfx/2008/xaml/mvvm   5      ?dxe5http://schemas.devexpress.com/winfx/2008/xaml/editors  5      ;dx2http://schemas.devexpress.com/winfx/2008/xaml/core  5	      8config+clr-namespace:Common.Config;assembly=Common  5
      c _DevExpress.Xpf.Ribbon.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a@dxr4http://schemas.devexpress.com/winfx/2008/xaml/ribbon   5      <dxb2http://schemas.devexpress.com/winfx/2008/xaml/bars  5      Alocal5clr-namespace:NetErp.Inventory.MeasurementUnits.Views  5      H DBusyIndicator, Version=2.1.0.0, Culture=neutral, PublicKeyToken=nullBbusyIndicator.https://github.com/moh3ngolshani/BusyIndicator  5      ^ ZCaliburn.Micro.Platform, Version=4.0.0.0, Culture=neutral, PublicKeyToken=8e5891231f2ed21f"calhttp://caliburnmicro.com  5        � 	Resources  5         PlaceHolderStyle �� 
TargetType �� Property �� Value*  $System.Windows.Controls.DataGridCell 3 /HorizontalAligment_Left_VerticalAligment_Center �� 
TargetType   BooleanToVisibilityConverter��%�  &	        &	 �     &	 �    �� 5   
   " ��6)   .�5      ��  � ��$ 14(�6)   �� 5       �@J�Opacity$	 0.5Q�6(   �� 5       � ��$ Oblique$�6*   �� 5       � ��$ Center=�62   5      �� 5   
   "  6   .�5      ��  � �� 5      �� 5      "  6&   .R�5      � #��z��6   .+�5      �� $��Center=�6/   $
��Left=�6J   $��3 0 0 0q�6e   5       5!      5#      5$      �� 5%   
   6'   5&      .��5(      � $
��auto��6   $
��auto��6    � RowDefinitions 5)   
   �� 5*      $
��auto �6   �� 5,      $
��auto �6   �� 5.      $
��auto �6   �� 50      $��* �6   �� 52      $
��auto �6   .+�57   
   )  #DevExpress.Xpf.Ribbon.RibbonControl  $��0��6   $��3��6,      IsSimplified$ False��6@      AllowSimplifiedRibbon$
 True��58         ShowApplicationButton$ False��61   	   ToolbarShowMode$
	 Hide=�6O   
   RibbonStyle$
 
Office2010=�59      %   ToolbarShowCustomizationButton$ False��6-      RibbonTitleBarVisibility$ 	Collapsed=�6T   $��Center=�6y      Items. 5;      &   DevExpress.Xpf.Ribbon.RibbonPage     Caption)  #DevExpress.Xpf.Core.ObjectConverter$ General 6      Groups. 5=      +  %DevExpress.Xpf.Ribbon.RibbonPageGroup     Caption$ Acciones��6&      Items. 5>      '  !DevExpress.Xpf.Bars.BarButtonItem  !  DevExpress.Xpf.Bars.BarItem   Content$ Nuevo 5@   $      RibbonStyle$ Large=�5A   $      Hint$&  Crear una nueva unidad de medida 5C   $      Glyph 5>   (   *  $DevExpress.Xpf.Core.DXImageExtension     Image1	  +DevExpress.Xpf.Core.Native.DXImageConverter$ New_16x16.png	    
LargeGlyph 5?   $     $ New_32x32.png	  �� 	IsEnabled 5B   $   ��  �� Path$ IsBusy� �� Mode$ OneWay=� �� UpdateSourceTrigger$ PropertyChanged=� �� 	Converter >
  8DevExpress.Mvvm.UI.BooleanToVisibilityConverterExtension
   
  Inverse$
 True��2  ,DevExpress.Mvvm.UI.Interactivity.Interaction   	Behaviors 5D      '  !DevExpress.Mvvm.UI.EventToCommand  5E      �  �DevExpress.Mvvm.UI.Interactivity.EventTriggerBase`1[[System.Windows.DependencyObject, WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]   	EventName$ 	ItemClick��6j   +  %DevExpress.Mvvm.UI.EventToCommandBase    Command  64   �� $" CreateMeasurementUnitCommand�6j   5G      *  $DevExpress.Xpf.Bars.BarItemSeparator  5H        5I      $ Eliminar 6(   !   Description$! Eliminar 5L   $   $/ )Eliminar la unidad de medida seleccionada 5M   $   $ Large=�5O   $    5J   $     $ DeleteList_16x16.png	  5K   $     $ DeleteList_32x32.png	  5N   $   �� $ CanDeleteMeasurementUnit�$ OneWay=�$ PropertyChanged=� 5P        5Q      $ 	ItemClick��6j     64   �� $" DeleteMeasurementUnitCommand�6j   5S      .  (DevExpress.Xpf.Bars.BarItemLinkSeparator  5T      5U      5V      5W      �� 5Z   
   $��2��6   " �� Orientation$" Vertical=�6"   .+�5[      %  DevExpress.Xpf.Editors.TextEdit  -   5        #  Name$# FilterSearch��5[      %  DevExpress.Xpf.Editors.BaseEdit$   NullText$<$ 6Escribe por lo menos 3 letras para iniciar la busqueda��5]       %   ShowNullTextForEmptyValue$
% True��5^      &   ShowNullTextIfFocused$
& True��5_      '   EditValuePostMode$' Delayed=�5`      (   CharacterCasing$( Upper=�5a      )   EditValuePostDelay$
) 1000��5c      *   	EditValue* 5\      �� $ FilterSearch�$ TwoWay=�$ PropertyChanged=���5b      ��    Common.Config.UICustom+   LabelFontSize, �� Source#, Z+ $ OneWay=�$ PropertyChanged=�5c      5d        BusyIndicator.BusyMask  5f   
   -   5        -  Name$- BusyIndicator��5f   !   .   IndicatorType$. BouncingDot=�5h      /   BusyContent$/ Please wait...��5i      0   BusyContentMargin$0 0,20,0,0q�5j      1   IsBusyAtStartup$1 False��5k      $��3��5l      2   IsBusy2 5g      �� *IsBusy+.��5n      % 	 DevExpress.Xpf.Grid.GridControl  $��0 8 0 0q�5o      ) 
 #DevExpress.Xpf.Grid.DataControlBase#3   EnableSmartColumnsGeneration$
3 True��5p      4   AutoGenerateColumns$
4 None=�5s      5   SelectedItem5 5q      �� $ SelectedMeasurementUnit�$ TwoWay=�$ PropertyChanged=�6   ItemsSource6 5r      �� $ MeasurementUnits�$ TwoWay=�$ PropertyChanged=�7   View7 5u      # 	 DevExpress.Xpf.Grid.TableView  5v      8   AllowCascadeUpdate$8 False��6$   & 
  DevExpress.Xpf.Grid.DataViewBase"9   EnableSelectedRowAppearance$
9 True��5w      & 	  DevExpress.Xpf.Grid.GridViewBase:   ShowGroupPanel$: False��5x      ;   AlternateRowBackground
; �����64   <   Columns.< 5|      $ 	 DevExpress.Xpf.Grid.GridColumn  $ 
 DevExpress.Xpf.Grid.BaseColumn=   Header$= Abreviación 5}      >   Width2 
 ,DevExpress.Xpf.Grid.GridColumnWidthConverter$> * 5~      $ 
 DevExpress.Xpf.Grid.ColumnBase?   Binding? 5      �� *Abbreviation+$ OneWay=�$ PropertyChanged=�  5�      $= Descripción 5�      $> * 5�      ? 5�      �� *Name+$ OneWay=�$ PropertyChanged=�  5�      $
> auto 6!   @   CellTemplate@ 5�      �� 5�      .R�5�      �� $��0q�5�      $��3q�5�      $��3q�5�      $
��auto��5�      $
��auto��6'     Caliburn.Micro.MessageA   Attach$2A ,[Event Click] = [Action EditMeasurementUnit]��5�      ��5�   "   �� 5�   &   $
��None=�6�   ��6,   1  +DevExpress.Xpf.Core.SvgImageSourceExtension  
B   Uri$\B Vpack://application:,,,/DevExpress.Images.v23.2;component/SvgImages/XAF/Action_Edit.svg?�5   /DevExpress.Xpf.Core.BaseSvgImageSourceExtensionC    Size	C 16,166�   5�      5�      5�      5�      5�      5�      5�      