   M S B A M L   `   `   `  ����A  =NetErp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullM+clr-namespace:NetErp.Global.CostCenters.DTONetErp.Global.CostCenters.DTO  5      /clr-namespace:NetErp.HelpersNetErp.Helpers  5      [2clr-namespace:NetErp.Global.CostCenters.ViewModels$NetErp.Global.CostCenters.ViewModels  5      O Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem 5      Q-clr-namespace:NetErp.Global.CostCenters.ViewsNetErp.Global.CostCenters.Views  5      :    4NetErp.Global.CostCenters.Views.CostCenterMasterView   5      a ]DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aW SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      =model+clr-namespace:NetErp.Global.CostCenters.DTO     5      *hclr-namespace:NetErp.Helpers     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      ^	 ZCaliburn.Micro.Platform, Version=4.0.0.0, Culture=neutral, PublicKeyToken=8e5891231f2ed21f"calhttp://caliburnmicro.com 	 5      ;dx2http://schemas.devexpress.com/winfx/2008/xaml/core  5	      c
 _DevExpress.Xpf.Ribbon.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a@dxr4http://schemas.devexpress.com/winfx/2008/xaml/ribbon  
 5
      D	viewmodel2clr-namespace:NetErp.Global.CostCenters.ViewModels  
 5      <dxb2http://schemas.devexpress.com/winfx/2008/xaml/bars  5      0sys&clr-namespace:System;assembly=mscorlib  5      ?dxe5http://schemas.devexpress.com/winfx/2008/xaml/editors  5      a ]DevExpress.Xpf.Grid.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49af bDevExpress.Xpf.Grid.v23.2.Core, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49al hDevExpress.Xpf.Grid.v23.2.Extensions, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a@dxg2http://schemas.devexpress.com/winfx/2008/xaml/grid    5      H DBusyIndicator, Version=2.1.0.0, Culture=neutral, PublicKeyToken=nullBbusyIndicator.https://github.com/moh3ngolshani/BusyIndicator  5      _ [Microsoft.Xaml.Behaviors, Version=1.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a:behavior+http://schemas.microsoft.com/xaml/behaviors  5      ] YDevExpress.Mvvm.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aAdxmvvm2http://schemas.devexpress.com/winfx/2008/xaml/mvvm   5      ;local-clr-namespace:NetErp.Global.CostCenters.Views   5        � 	Resources  5      !   NetErp.Helpers.BindingProxy   DataContextProxy   Data-   'NetErp.Helpers.StringToBooleanConverter  StringToBooleanConverter �� DataType.   (NetErp.Global.CostCenters.DTO.CompanyDTO �� ItemsSource �� Mode �� UpdateSourceTrigger �� Orientation � ContextMenu m� Command	 �� Source
 m� Icon*  $DevExpress.Xpf.Core.DXImageExtension �� 
TargetType �� Triggers ^� 
Conditions �� Binding �� Value �� Property �� Value6   0NetErp.Global.CostCenters.DTO.CompanyLocationDTO6   0NetErp.Global.CostCenters.DTO.CostCenterDummyDTO3   -NetErp.Global.CostCenters.DTO.StorageDummyDTO1   +NetErp.Global.CostCenters.DTO.CostCenterDTO.	   (NetErp.Global.CostCenters.DTO.StorageDTO��%J  &	        &	      (�� /     5   #   *, +)7   (�� a    5E   #   *, +)7   (�� �    5w   #   *, +)7   (�� �    5�   #   *, +)7   (�� �    5�      *, +)7   (�� �    5�      *,	 +)7     5   
    62   ��   5   
   6%   �� 5   
   "  6#    6H   �� *	Locations+$ TwoWay=�$ PropertyChanged=�.R�5      �� $ 
Horizontal=�6   $��-1,0,0,0q�62   $��Center=�6D   
������  5      �� 5      .@�5      m� $,��&Crear agrupación de centros de costos��6#    6R   �� *#!Data.CreateCompanyLocationCommand+8	   
 5      �� 5   "   ��6(     *" Images/Actions/AddItem_16x16.png+5!      5"      .+�5$      �� $��18��6   $��18��6#   $��Center=�6/   ��5%      �� 5&      " ��6     5'      ^� 5+   "    5,   &   �� 5-   *    64   �� *Row.IsExpanded+	 false6W   .0�52   &   ��  � �� 6?     *0.SvgImages/Icon Builder/Actions_FolderClose.svg+53   #   ^� 57   "    58   &   �� 59   *    64   �� *Row.IsExpanded+ true6W   .0�5;   &   ��  � �� 6?     */-SvgImages/Icon Builder/Actions_FolderOpen.svg+5<   #   5>      5@      �� 5A      $��Center=�6�   ��6   �� *(&Row.AccountingEntityCompany.SearchName+$ PropertyChanged=�$ TwoWay=�6�   5B      5C      �� 5E   
   "  6#    6P   �� *
DummyItems+$ TwoWay=�$ PropertyChanged=�.R�5F      �� $ 
Horizontal=�6   $��-1,0,0,0q�62   $��Center=�6D   
������  5G      �� 5H      .@�5I      m�  6#   �� *#!Data.DeleteCompanyLocationCommand+8	   ��5J      �� 5K   "   $ 
Horizontal=�6-   .+�5L   &   �� $1��+Eliminar agrupación de centros de costos: ��60   �� 5M   &   ��60   �� *
Row.Name+5N   #   
 5P      �� 5Q   "   ��6(     *Images/Edit/Delete_16x16.png+5S      5T      .+�5V      �� $��18��6   $��18��6#   $��Center=�6/   ��5W      �� 5X      " ��6     5Y      ^� 5]   "    5^   &   �� 5_   *    64   �� *Row.IsExpanded+	 false6W   .0�5d   &   ��  � �� 6?     *0.SvgImages/Icon Builder/Actions_FolderClose.svg+5e   #   ^� 5i   "    5j   &   �� 5k   *    64   �� *Row.IsExpanded+ true6W   .0�5m   &   ��  � �� 6?     */-SvgImages/Icon Builder/Actions_FolderOpen.svg+5n   #   5p      5r      �� 5s      $��Center=�6h   ��6   �� *
Row.Name+$ PropertyChanged=�$ TwoWay=�6h   5t      5u      �� 5w   
   "  6#    6P   �� *CostCenters+$ TwoWay=�$ PropertyChanged=�.R�5x      �� $ 
Horizontal=�6   $��-1,0,0,0q�62   $��Center=�6D   
������  5y      �� 5z      .@�5{      m� $��Crear centro de costos��6#    6C   �� *Data.CreateCostCenterCommand+8	   
 5|      �� 5}   "   ��6(     *" Images/Actions/AddItem_16x16.png+5      5�      .+�5�      �� $��18��6   $��18��6#   $��Center=�6/   ��5�      �� 5�      " ��6     5�      ^� 5�   "    5�   &   �� 5�   *    64   �� *Row.IsExpanded+	 false6W   .0�5�   &   ��  � �� 6?     *0.SvgImages/Icon Builder/Actions_FolderClose.svg+5�   #   ^� 5�   "    5�   &   �� 5�   *    64   �� *Row.IsExpanded+ true6W   .0�5�   &   ��  � �� 6?     */-SvgImages/Icon Builder/Actions_FolderOpen.svg+5�   #   5�      5�      �� 5�      $��Center=�6h   ��6   �� *
Row.Name+$ PropertyChanged=�$ TwoWay=�6h   5�      5�      �� 5�   
   "  6#    6M   �� *
Storages+$ TwoWay=�$ PropertyChanged=�.R�5�      �� $ 
Horizontal=�6   $��-1,0,0,0q�62   $��Center=�6D   
������  5�      �� 5�      .@�5�      m� $��Crear bodega��6#    69   �� *Data.CreateStorageCommand+8	   
 5�      �� 5�   "   ��6(     *" Images/Actions/AddItem_16x16.png+5�      5�      .+�5�      �� $��18��6   $��18��6#   $��Center=�6/   ��5�      �� 5�      " ��6     5�      ^� 5�   "    5�   &   �� 5�   *    64   �� *Row.IsExpanded+	 false6W   .0�5�   &   ��  � �� 6?     *0.SvgImages/Icon Builder/Actions_FolderClose.svg+5�   #   ^� 5�   "    5�   &   �� 5�   *    64   �� *Row.IsExpanded+ true6W   .0�5�   &   ��  � �� 6?     */-SvgImages/Icon Builder/Actions_FolderOpen.svg+5�   #   5�      5�      �� 5�      $��Center=�6h   ��6   �� *
Row.Name+$ PropertyChanged=�$ TwoWay=�6h   5�      5�      �� 5�   
   "  6   .R�5�      �� $ 
Horizontal=�6   $��-1,0,0,0q�62   $��Center=�6D   
������  5�      �� 5�      .@�5�      m�  6#   �� *Data.DeleteCostCenterCommand+8	   ��5�      �� 5�   "   $ 
Horizontal=�6-   .+�5�   &   �� $!��Eliminar centro de costos: ��60   �� 5�   &   ��60   �� *
Row.Name+5�   #   
 5�      �� 5�   "   ��6(     *Images/Edit/Delete_16x16.png+5�      5�      .+�5�      �� $��18��6   $��18��6#   $��Center=�6~   ��6/     *86Images/Miscellaneous/HighlightActiveElements_16x16.png+6~   �� 5�      $��2 0 0 0q�6h   $��Center=�6y   ��6   �� *
Row.Name+$ PropertyChanged=�$ TwoWay=�6y   5�      5�      �� 5�   
   " 	 6   .R�5�      �� $ 
Horizontal=�6   $��-1,0,0,0q�62   $��Center=�6D   
������  5�      �� 5�      .@�5�      m�  6#   �� *Data.DeleteStorageCommand+8	   ��5�      �� 5�   "   $ 
Horizontal=�6-   .+�5�   &   �� $��Eliminar bodega: ��60   �� 5�   &   ��60   �� *
Row.Name+5�   #   
 5�      �� 5�   "   ��6(     *Images/Edit/Delete_16x16.png+5�      5�      .+�5�      �� $��18��6   $��18��6#   $��Center=�6~   ��6/     *86Images/Miscellaneous/HighlightActiveElements_16x16.png+6~   �� 5�      $��2 0 0 0q�6h   $��Center=�6y   ��6   �� *
Row.Name+$ PropertyChanged=�$ TwoWay=�6y   5�      5�      5�      .��5�      
  BusyIndicator.BusyMask
  -   5         
 Name$ BusyIndicator��5�       
  IndicatorType$ BouncingDot=�5�       
  BusyContent$ Please wait...��5�       
  BusyContentMargin$ 0,20,0,0q�5�       
  IsBusyAtStartup$ False��5       
  IsBusy 5�      �� *IsBusy+.��5  
   �  � ColumnDefinitions 5     �� 5     $
��0.4* �6#   �� 5     $��* �6#   .+�5     )  #DevExpress.Xpf.Grid.TreeViewControl  $��0 10 0 0q�6"   $��0��64   &    NetErp.Helpers.TreeViewExtension   SelectItemOnRightClick$
 True��5	  *   $
��Auto=�5
  *   $��Disabled=�5  *   "   HighlightSelectedNodeIndent$ False��5  *   $��0q�5  *      ShowExpandButtons$
 True��5  *   )  #DevExpress.Xpf.Grid.DataControlBase   
ShowBorder$ False��5  *      TreeDerivationMode$ HierarchicalDataTemplate=�5  *      ItemsSource 5  *   �� *	Companies+$ TwoWay=�$ PropertyChanged=�   ExpandStateBinding 5  *   �� *
IsExpanded+$ TwoWay=�$ PropertyChanged=�    SelectedItem  5  *   �� *SelectedItem+$ TwoWay=�$ PropertyChanged=�{�5  *   �� *TreeViewIsEnable+$ PropertyChanged=�5     � 5     $��1��6   ! � RowDefinitions! 5     �� 5     $��0.13* �6$   �� 5     $��* �6$   .+�5     ) 
 #DevExpress.Xpf.Ribbon.RibbonControl  $��0��5     $��
20 10 0 10q�6&   "   IsSimplified$" False��6:   #   AllowSimplifiedRibbon$
# True��5     $   ShowApplicationButton$$ False��69   %   ToolbarShowMode$
% Hide=�6W   &   RibbonStyle$& 
Office2010=�5     %'   ToolbarShowCustomizationButton$' False��65   (   RibbonTitleBarVisibility$( 	Collapsed=�6\   $��Center=�6�   #)   MinimizationButtonVisibility$) 	Collapsed=�5     y�5  $   �� *TabControlVisibility+* �� 	Converter* ?  9DevExpress.Xpf.Core.BooleanToVisibilityConverterExtension  $ PropertyChanged=�+   Items.+ 5      & 
  DevExpress.Xpf.Ribbon.RibbonPage  ,   Caption)  #DevExpress.Xpf.Core.ObjectConverter$, Opciones 6%   -   Groups.- 5!     + 
 %DevExpress.Xpf.Ribbon.RibbonPageGroup  .   Caption$. Acciones��6.   /   Items./ 5"     '  !DevExpress.Xpf.Bars.BarButtonItem  !  DevExpress.Xpf.Bars.BarItem0   Content$0 Editar 61   1   RibbonStyle$1 SmallWithText=�6B   2   Glyph2 6^     *Images/Edit/Edit_16x16.png+3 �� 	IsEnabled3 5#  -   �� *	CanEdit+$ TwoWay=�$ PropertyChanged=�2  ,DevExpress.Mvvm.UI.Interactivity.Interaction4   	Behaviors4 5$  "   '  !DevExpress.Mvvm.UI.EventToCommand  5%  &   �  �DevExpress.Mvvm.UI.Interactivity.EventTriggerBase`1[[System.Windows.DependencyObject, WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]5   	EventName$5 	ItemClick��6\   +  %DevExpress.Mvvm.UI.EventToCommandBase6   Command6 6<   �� *EditCommand+6\   5'     *  $DevExpress.Xpf.Bars.BarItemSeparator  5(       5)     $0 Deshacer 60   $1 SmallWithText=�6C   2 6_     *Images/History/Undo_16x16.png+3 5*  0   �� *	CanUndo+$ TwoWay=�$ PropertyChanged=�4 5+  "     5,  &   $5 	ItemClick��6\   6 6<   �� *UndoCommand+6\   5.       5/       50     $0 Guardar 60   $1 SmallWithText=�6B   2 6^     *Images/Save/Save_16x16.png+3 51  0   �� *	CanSave+$ PropertyChanged=�4 52  "     53  &   $5 	ItemClick��6\   6 6<   �� *SaveCommand+6\   55     56     57     58     �� 5:     $��1��5;     ��5:  !   �� *SelectedItem+$ PropertyChanged=�y�5<     �� *TabControlVisibility+*   $ PropertyChanged=�  5=     &   DevExpress.Xpf.Core.DXTabControl7   TabContentCacheMode�  �DevExpress.Xpf.Core.Native.SelectorBase`2[[DevExpress.Xpf.Core.DXTabControl, DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a],[DevExpress.Xpf.Core.DXTabItem, DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a]]8   SelectedIndex#  DevExpress.Xpf.Core.DXTabItem�  �DevExpress.Xpf.Core.Native.HeaderedSelectorItemBase`2[[DevExpress.Xpf.Core.DXTabControl, DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a],[DevExpress.Xpf.Core.DXTabItem, DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a]]9   Header%  DevExpress.Xpf.Editors.TextEdit%  DevExpress.Xpf.Editors.BaseEdit:   
IsReadOnly)   #DevExpress.Xpf.Editors.TextEditBase;    Text< �� Path= P� Property> P� Value?  Name@   CharacterCasingA 	� 	GroupNameB d� 	IsCheckedC �� ConverterParameterD    	MaxLengthE   MaskTypeF   Mask+!  %DevExpress.Xpf.Grid.LookUp.LookUpEditG   	AllowSpinH !  AutoPopulateColumns+"  %DevExpress.Xpf.Editors.LookUpEditBaseI "  IsCaseSensitiveSearchJ "  IncrementalFilteringK "  IncrementalSearchL   ValidateOnTextInputM "  FilterConditionN "  ImmediatePopup O "  ClearSelectionOnBackspaceP "  DisplayMemberQ "  ValueMemberR "  ItemsSourceS "  SelectedItem*#  $DevExpress.Xpf.Editors.PopupBaseEditT #  PopupContentTemplate%$  DevExpress.Xpf.Grid.GridControlU $  View#%  DevExpress.Xpf.Grid.TableViewV %  	AutoWidthW $  Columns$&  DevExpress.Xpf.Grid.GridColumn$'  DevExpress.Xpf.Grid.BaseColumnX '  Header$(  DevExpress.Xpf.Grid.ColumnBaseY (  	FieldNameZ "  
ItemsPanel))  #DevExpress.Xpf.Editors.ComboBoxEdit'*  !DevExpress.Xpf.Editors.ButtonEdit[ *  IsTextEditable\   	EditValue%+  DevExpress.Xpf.Editors.SpinEdit] +  	Increment^ +  MinValue_   AllowSpinOnMouseWheel` +  IsFloatValuea   EditValueType��%G  (��       5?  '   *, +)7   7   7   7   (�� �    5q  '   *, +)7   7   (��     5�  '   *, +)7   7   7   7  7   7  7   7  7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   7   (�� �4    5�  '   *,	 +)7   7   7   7  7   7  7   7  7   7   7   7   7   7   7   7   �� 5?     "  6'   .R�5@       
������ $7 CacheTabsOnSelecting=�5A  *   $��Stretch=�5B  *   $��20 0 0 0q�5C  *   8 5D  *   �� *Data.SelectedIndex+$ PropertyChanged=�8	   .@�5E  "     $9 Basicos��6/   .��5F  &   � {�6+   �� *Data.IsEditing+$ PropertyChanged=�8	   5G  *   �� 5H  .   $��* �6?   �� 5I  .   $��* �6?   �� 5J  .   $��* �6?   ! 5L  *   �� 5M  .   $
��auto �6<   �� 5N  .   $
��auto �6<   �� 5O  .   $
��auto �6<   .+�5Q  *   �� $��Razón social��64   $��0��6H   $��0��6X   $��0 5 0 5q�6e   � 5R  *   $��0��6/   $��1��6?   $��3��6L    5S  .   �� 5T  2   $
��8.0* �6C   �� 5U  2   $��* �6C   .+�5W  .     $��0��5X  #   $��0 5 5 5q�63   $
: True��6D   ; 5W  ;   �� */-Data.CompanyAccountingEntityCompanySearchName+$ PropertyChanged=�$ TwoWay=�8	  5X  D   �� 5Y  .   $��1��65   $��5 5 0 5q�6E   $
��auto��6V   ��6c   �� $6< 0Data.SearchCompanyAccountingEntityCompanyCommand�8	  .��5Z  2   �� $ 
Horizontal=�6=   .+�5[  6   �� $��Buscar��6@   $��Center=�6N   $��0 0 5 0q�6i   �� 5\  6   $
��None=�6o   ��6<     *Images/Find/Find_16x16.png+��5]  :   �� 5^  >   " ��6D    5_  B   P� 5`  F   = � {�$> False��6c   . �5a  J   ��  �@J�Opacity$ .5Q�6d   5b  G   5d  ?   5f  7   5g  3   5h  /   5i  +   5j  '   5l  #   5m     5n     �� 5q     "  6'   .R�5r     � {�6#   �� *Data.IsEditing+$ PropertyChanged=�8	    5s  "   �� 5t  &   $��* �67   �� 5u  &   $��5* �67   �� 5v  &   $��* �67   ! 5x  "   �� 5y  &   $
��auto �64   �� 5z  &   $
��auto �64   �� 5{  &   $
��auto �64   �� 5|  &   $
��auto �64   .+�5~  "   �� $"��Agrupación de la compañía��6,   $��1��6M   $��0��6]   $��Center=�6j   $��40(�6�   �� 5�  "   $����Lorem Ipsum es simplemente el texto de relleno de las imprentas y archivos de texto. Lorem Ipsum ha sido el texto de relleno estándar de las industrias desde el año 1500, cuando un impresor (N. del T. persona que se dedica a la imprenta) desconocido usó una galería de textos y los mezcló de tal manera que logró hacer un libro de textos especimen. No sólo sobrevivió 500 años, sino que tambien ingresó como texto de relleno en documentos electrónicos, quedando esencialmente igual al original. Fue popularizado en los 60s con la creación de las hojas.��6,   $��1��5�  (   $��1��68   $
��Wrap=�6E   $��10q�6Y   �� 5�  "   $��Nombre��6,   $��1��6:   $��2��6J     5�  "   $? CompanyLocationName��5�  `   $��1��6+   $��3��6;   $@ Upper=�6H   ; 5�  /   �� *Data.CompanyLocationName+8	  $ TwoWay=�$ PropertyChanged=�5�  `   5�     5�     �� 5�     "  6'   .R�5�       
������ $7 CacheTabsOnSelecting=�5�     $��Stretch=�5�     $��20 0 0 0q�5�     8 5�     �� *Data.SelectedIndex+$ PropertyChanged=�8	   .@�5�  "     $9 Basicos��6/   .��5�  &   � {�6+   �� *Data.IsEditing+$ PropertyChanged=�8	   5�  *   �� 5�  .   $��* �6?   �� 5�  .   $��* �6?   �� 5�  .   $��* �6?   �� 5�  .   $��* �6?   ! 5�  *   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   .+�5�  *   �� $ 
Horizontal=�65   $��0��6N   $��0��6[   $��2��6k   .+�5�  .   	� $��Activo��6:   $A State��6K   B 6]   �� *Data.CostCenterState+8	  8*  $C A��$ PropertyChanged=�	� 5�  .   $��Solo Consultas��6:   $A State��6S   $��5 0 5 0q�6*  B 6e   �� *Data.CostCenterState+8	  8*  $C R��$ PropertyChanged=�6*  	� 5�  .   $��Inactivo��6:   $A State��6M   B 6_   �� *Data.CostCenterState+8	  8*  $C I��$ PropertyChanged=�5�  +   �� 5�  *   $��Nombre del almacén��64   $��1��6N   $��0��6[   $��0 5 0 5q�6k     5�  *   $? CostCenterName��5�  �   $��2��63   $��0��6@   $��4��6P   $��0 0 5 0q�6d   $@ Upper=�6u   ; 5�  7   �� *Data.CostCenterName+8	  $ PropertyChanged=�$ TwoWay=�5�  �   �� 5�  *   $/��)Nombre comercial (aplicado en la factura)��64   $��0 5 0 5q�6e   $��3��6v   $��0��6�   $��2��6�     5�  *   $��4��5�     $��3��6)   $��0 0 5 0q�6=   $@ Upper=�6N   ; 5�  7   �� *Data.CostCenterTradeName+8	 	 $ PropertyChanged=�$ TwoWay=�5�  N   �� 5�  *   $��Nombre corto��64   $��5 5 0 5q�6H   $��3��6Y   $��3��6f     5�  *   $��4��5�     $��4��6)   $��5 0 5 0q�69   $@ Upper=�6J   ; 5�  7   �� *Data.CostCenterShortName+8	 
 $ PropertyChanged=�$ TwoWay=�5�  J   �� 5�  *   $��Telefono fijo No. 1��64   $��0��6O   $��5��6_   $��0 5 0 5q�6l     5�  *   $��0��5�     $��6��6,   $��0 0 5 0q�69   $D 10��6J   $E Simple=�6Y   $F 	000 00 00��6k   ; 5�  7   �� *Data.CostCenterPhone1+8	  $ PropertyChanged=�$ TwoWay=�5�  k   �� 5�  *   $��Telefono fijo No. 2��64   $��1��6O   $��5��6_   $��5 5 0 5q�6l     5�  *   $��1��5�     $��6��6,   $��5 0 5 0q�69   $D 10��6J   $E Simple=�6Y   $F 	000 00 00��6k   ; 5�  7   �� *Data.CostCenterPhone2+8	  $ PropertyChanged=�$ TwoWay=�5�  k   �� 5�  *   $��Celular No. 1��64   $��2��6I   $��5��6Y   $��5 5 0 5q�6f     5�  *   $��6��5�     $��2��6)   $��5 0 5 0q�69   $D 14��6J   $E Simple=�6Y   $F 000 000 00 00��6k   ; 5�  7   �� *Data.CostCenterCellPhone1+8	  $ PropertyChanged=�$ TwoWay=�5�  k   �� 5�  *   $��Celular No. 2��64   $��3��6I   $��5��6Y   $��5 5 0 5q�6f     5�  *   $��6��5�     $��3��6)   $��5 0 5 0q�69   $D 14��6J   $E Simple=�6Y   $F 000 000 00 00��6k   ; 5�  7   �� *Data.CostCenterCellPhone2+8	  $ PropertyChanged=�$ TwoWay=�5�  k   �� 5�  *   $��País��64   $��0��6@   $��7��6P   $��0 5 0 5q�6]   !  5�  *   $��0��69   $��4��6I   $��8��6]   $��0 0 5 0q�5�  5   $��3q�6F   $
G True��5�  5   $H False��5�  5   $I False��5�  5   J True5�  5   $
K True��5�  5   $L False��5�  5   M Contains5�  5   N True5�  5   $
O True��5�  5   $
P Name��5�  5   $Q Id��5�  5   R 5�  5   �� *Data.Countries+8	  $ TwoWay=�$ PropertyChanged=�S 5�  5   �� * Data.CostCenterSelectedCountry+8	  $ TwoWay=�$ PropertyChanged=�T 5�  .   �� 5�  2   .R�5�  6   $  $��PART_GridControl��6F   U 5�  :   %  5�  >   $
V True��6L   W 5�  :   &  5�  >   $X Nombre del pais 6M   $
Y Name��6f   5�  7   5�  3   Z 5�  .   �� 5�  2   .R�5�  6   $� 5�  3   5�  +   �� 5�  *   $��Departamento��64   $��0��6H   $��9��6X   $��0 5 0 5q�6e   !  5�  *   $��0��69   $��4��6I   $��10��6]   $��0 0 5 0q�5      $��3q�6   $
G True��5     $H False��5     $I False��5     J True5     $
K True��5     $L False��5     M Contains5     N True5     $
O True��5	     $
P Name��5     $Q Id��5     R 5
     �� *,*Data.CostCenterSelectedCountry.Departments+8	  $ TwoWay=�$ PropertyChanged=�S 5     �� *#!Data.CostCenterSelectedDepartment+8	  $ TwoWay=�$ PropertyChanged=�T 5  .   �� 5  2   .R�5  6   $  $��PART_GridControl��6F   U 5  :   %  5  >   $
V True��6L   W 5  :   &  5  >   $X Nombre del departamento 6M   $
Y Name��6n   5  7   5  3   Z 5  .   �� 5  2   .R�5  6   $� 5  3   5  +   �� 5"  *   $��Ciudad��64   $��0��6B   $��11��6R   $��0 5 0 5q�6`   !  5#  *   $��0��69   $��4��6I   $��12��6]   $��0 0 5 0q�5$     $��3q�6   $
G True��5%     $H False��5&     $I False��5'     J True5(     $
K True��5)     $L False��5*     M Contains5+     N True5,     $
O True��5-     $
P Name��5/     $Q Id��50     R 5.     �� **(Data.CostCenterSelectedDepartment.Cities+8	  $ TwoWay=�$ PropertyChanged=�S 51     �� *Data.CostCenterSelectedCity+8	  $ TwoWay=�$ PropertyChanged=�T 52  .   �� 53  2   .R�54  6   $  $��PART_GridControl��6F   U 55  :   %  56  >   $
V True��6L   W 58  :   &  59  >   $X Nombre de la ciudad 6M   $
Y Name��6j   5;  7   5<  3   Z 5>  .   �� 5?  2   .R�5@  6   $� 5A  3   5C  +   �� 5F  *   $��
Dirección��64   $��0��6E   $��13��6U   $��0 5 0 5q�6c     5G  *   $��14��5H     $��0��6*   $��4��6:   $��0 0 5 0q�6N   ; 5G  7   �� *Data.CostCenterAddress+8	  $ PropertyChanged=�$ TwoWay=�5H  N   �� 5K  *   $-��'Control de fecha para las transacciones��64   $��0��6c   $��15��6s   $��0 5 0 5q�6�   $��2��6�   )  5L  *   $��0��6;   $��4��6K   $��16��6_   $��0 0 5 0q�5M  -   	[ False5N  -   $P Value��5P  -   $	Q Key��5Q  -   R 5O  -   �� **(Data.CostCenterDateControlTypeDictionary+8	  $ PropertyChanged=�\ 5R  -   �� *(&Data.SelectedCostCenterDateControlType+8	  $ TwoWay=�$ PropertyChanged=�� 5T  *   $��0��6/   $��17��6?   $��4��6M   $��0 0 5 0q�6a    5U  .   �� 5V  2   $��* �6C   �� 5W  2   $��* �6C   �� 5X  2   $��* �6C   .+�5[  .   �� $8��2Mostrar ventana para calculo de cambio en efectivo��67   $��0��6t   $��0 5 0 5q�6�   $
��Left=�6�   B 5\  3   �� *'%Data.CostCenterShowChangeWindowOnCash+8	  $ PropertyChanged=�$ TwoWay=��� 5_  .   $&�� ¿Este almacén realiza compras?��67   $��1��6`   $��0 5 0 5q�6p   $��Center=�6�   B 5`  3   �� *Data.CostCenterAllowBuy+8	  $ PropertyChanged=�$ TwoWay=��� 5c  .   $%��¿Este almacén realiza ventas?��67   $��2��6_   $��0 5 0 5q�6o   $��Right=�6�   B 5d  3   �� *Data.CostCenterAllowSell+8	  $ PropertyChanged=�$ TwoWay=�5f  +   � 5h  *   $��0��6/   $��20��6?   $��0 5 0 5q�6M   $��2��6^    5i  .   �� 5j  2   $
��0.3* �6C   �� 5k  2   $��* �6C   ! 5m  .   �� 5n  2   $
��auto �6@   �� 5o  2   $
��auto �6@   .+�5r  .   �� $��¿Factura IVA?��67   $��0��6O   $��0��6_   B 5s  3   �� *Data.CostCenterIsTaxable+8	  $ PropertyChanged=�$ TwoWay=��� 5u  .   $-��'¿Los precios en la lista incluyen IVA?��67   $��1��6h   $��0��6x   y�6�   �� *Data.CostCenterIsTaxable+8	  *   B 5v  3   �� *$"Data.CostCenterPriceListIncludeTax+8	  $ PropertyChanged=�$ TwoWay=��� 5x  .   $7��1¿Los precios en la factura impresa incluyen IVA?��67   $��1��6r   $��1��6   y�6�   �� *Data.CostCenterIsTaxable+8	  *   B 5y  3   �� *'%Data.CostCenterInvoicePriceIncludeTax+8	  $ PropertyChanged=�$ TwoWay=�5z  +   5{  '   5|  #     5}  "   $9 
Inventario��6/   .��5~  &   � {�6+   �� *Data.IsEditing+$ PropertyChanged=�8	   ! 5  *   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   .+�5�  *   �� $��Llevar IVA al costo��63   B 5�  /   �� *Data.CostCenterTaxToCost+8	 ! $ PropertyChanged=�$ TwoWay=��� 5�  *   $����Seleccione esta opción en caso de querer llevar IVA al costo del inventario (no es necesario si el almacén es regimen simplificado)��64   $��1��5�  0   ${�False��6=   5�  '   5�  #     5�  "   $9 Ventas��6/   .��5�  &   � {�6+   �� *Data.IsEditing+$ PropertyChanged=�8	 "  5�  *   �� 5�  .   $��* �6?   �� 5�  .   $��* �6?   ! 5�  *   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   .+�5�  *   �� $A��;¿Permitir repetir productos en la facturación a clientes?��63   $��0��6w   $��0��6�   B 5�  /   �� *(&Data.CostCenterAllowRepeatItemsOnSales+8	 # $ PropertyChanged=�$ TwoWay=��� 5�  *   $&�� Impresión de copias de facturas��64   $��0��6[   $��1��6k   $��0 5 0 5q�6x   �� 5�  *   $��0��65   $��2��6E   $ 
Horizontal=�6R   $��2��6k   $��0 5 0 5q�6   .+�5�  .   �� $��Imprimir��68   $��Center=�6H   $��0 0 5 0q�6c   +  5�  .   $] 1~�6;   ^ 06I   $_ False��6V   $` False��6t   "a ��5�  7   \ 5�  7   �� *%#Data.CostCenterInvoiceCopiesToPrint+8	 $ $ TwoWay=�$ PropertyChanged=��� 5�  .   $X��Rcopias(s) de la factura de venta (Deje la casilla en cero para no imprimir copias)��68   $��Center=�6�   $��5 0 0 0q�6�   5�  +   �� 5�  *   $5��/¿Pedir confirmación antes de imprimir copias?��63   $��3��6k   $��0��6x   {�5�  /   �� *:8Data.CostCenterRequiresConfirmationToPrintCopiesIsEnable+8	 % $ PropertyChanged=�B 5�  /   �� *20Data.CostCenterRequiresConfirmationToPrintCopies+8	 & $ TwoWay=�$ PropertyChanged=�5�  '   5�  #   5�     5�     �� 5�     " 	 6'   .R�5�       
������ $7 CacheTabsOnSelecting=�5�  *   $��Stretch=�5�  *   $��20 0 0 0q�5�  *   8 5�  *   �� *Data.SelectedIndex+$ PropertyChanged=�8	   .@�5�  "     $9 Basicos��6/   .��5�  &   � {�6+   �� *Data.IsEditing+$ PropertyChanged=�8	   5�  *   �� 5�  .   $��* �6?   �� 5�  .   $��* �6?   �� 5�  .   $��* �6?   �� 5�  .   $��* �6?   ! 5�  *   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   �� 5�  .   $
��auto �6<   .+�5�  *   �� $ 
Horizontal=�65   $��0��6N   $��0��6[   $��2��6k   .+�5�  .   	� $��Activo��6:   $A State��6K   B 6]   �� *Data.StorageState+8	  8*  $C A��$ PropertyChanged=�	� 5�  .   $��Solo Consultas��6:   $A State��6S   $��5 0 5 0q�6'  B 6e   �� *Data.StorageState+8	  8*  $C R��$ PropertyChanged=�6'  	� 5�  .   $��Inactivo��6:   $A State��6M   B 6_   �� *Data.StorageState+8	  8*  $C I��$ PropertyChanged=�5�  +   �� 5�  *   $��Nombre de la bodega��64   $��1��6O   $��0��6\   $��0 5 0 5q�6l     5�  *   $? StorageName��5�  u   $��2��63   $��0��6@   $��4��6P   $��0 0 5 0q�6d   ; 5�  7   �� *Data.StorageName+8	  $ PropertyChanged=�$ TwoWay=�5�  u   �� 5�  *   $��País��64   $��0��6@   $��3��6P   $��0 5 0 5q�6]   !  5�  *   $��0��69   $��4��6I   $��4��6]   $��0 0 5 0q�5�  5   $��3q�6F   $
G True��5�  5   $H False��5�  5   $I False��5�  5   J True5�  5   $
K True��5�  5   $L False��5�  5   M Contains5�  5   N True5�  5   $
O True��5�  5   $
P Name��5�  5   $Q Id��5�  5   R 5�  5   �� *Data.Countries+8	 	 $ TwoWay=�$ PropertyChanged=�S 5�  5   �� *Data.StorageSelectedCountry+8	 
 $ TwoWay=�$ PropertyChanged=�T 5�  .   �� 5�  2   .R�5�  6   $  $��PART_GridControl��6F   U 5�  :   %  5�  >   $
V True��6L   W 5�  :   &  5�  >   $X Nombre del pais 6M   $
Y Name��6f   5�  7   5�  3   Z 5�  .   �� 5�  2   .R�5�  6   $� 5�  3   5�  +   �� 5�  *   $��Departamento��64   $��0��6H   $��5��6X   $��0 5 0 5q�6e   !  5�  *   $��0��69   $��4��6I   $��6��6]   $��0 0 5 0q�5�  5   $��3q�6F   $
G True��5�  5   $H False��5�  5   $I False��5�  5   J True5�  5   $
K True��5�  5   $L False��5   5   M Contains5  5   N True5  5   $
O True��5  5   $
P Name��5  5   $Q Id��5  5   R 5  5   �� *)'Data.StorageSelectedCountry.Departments+8	  $ TwoWay=�$ PropertyChanged=�S 5  5   �� * Data.StorageSelectedDepartment+8	  $ TwoWay=�$ PropertyChanged=�T 5  .   �� 5	  2   .R�5
  6   $  $��PART_GridControl��6F   U 5  :   %  5  >   $
V True��6L   W 5  :   &  5  >   $X Nombre del departamento 6M   $
Y Name��6n   5  7   5  3   Z 5  .   �� 5  2   .R�5  6   $� 5  3   5  +   �� 5  *   $��Ciudad��64   $��0��6B   $��7��6R   $��0 5 0 5q�6_   !  5  *   $��0��69   $��4��6I   $��8��6]   $��0 0 5 0q�5  5   $��3q�6F   $
G True��5  5   $H False��5   5   $I False��5!  5   J True5"  5   $
K True��5#  5   $L False��5$  5   M Contains5%  5   N True5&  5   $
O True��5'  5   $
P Name��5)  5   $Q Id��5*  5   R 5(  5   �� *'%Data.StorageSelectedDepartment.Cities+8	  $ TwoWay=�$ PropertyChanged=�S 5+  5   �� *Data.StorageSelectedCity+8	  $ TwoWay=�$ PropertyChanged=�T 5,  .   �� 5-  2   .R�5.  6   $  $��PART_GridControl��6F   U 5/  :   %  50  >   $
V True��6L   W 52  :   &  53  >   $X Nombre de la ciudad 6M   $
Y Name��6j   55  7   56  3   Z 58  .   �� 59  2   .R�5:  6   $� 5;  3   5=  +   �� 5@  *   $��
Dirección��64   $��0��6E   $��9��6U   $��0 5 0 5q�6b     5A  *   $��10��5B  3   $��0��6A   $��4��6Q   $��0 0 5 0q�6e   ; 5A  7   �� *Data.StorageAddress+8	  $ PropertyChanged=�$ TwoWay=�5B  e   5D  '   5E  #   5F     5G     5H     5I     5T     5V     5W     5X     