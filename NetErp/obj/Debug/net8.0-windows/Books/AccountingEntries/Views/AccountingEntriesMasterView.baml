   M S B A M L   `   `   `  ����A  =NetErp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null[2clr-namespace:NetErp.Books.AccountingEntries.Views$NetErp.Books.AccountingEntries.Views  5      /clr-namespace:NetErp.HelpersNetErp.Helpers  5      9!clr-namespace:NetErp.UserControlsNetErp.UserControls  5      F    @NetErp.Books.AccountingEntries.Views.AccountingEntriesMasterView   5      a ]DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aW SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      Dlocal2clr-namespace:NetErp.Books.AccountingEntries.Views     5      *hclr-namespace:NetErp.Helpers     5      0uc!clr-namespace:NetErp.UserControls     5      c _DevExpress.Xpf.Ribbon.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a@dxr4http://schemas.devexpress.com/winfx/2008/xaml/ribbon   5	      ]	 YDevExpress.Mvvm.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aAdxmvvm2http://schemas.devexpress.com/winfx/2008/xaml/mvvm 	  5
      ?dxe5http://schemas.devexpress.com/winfx/2008/xaml/editors  5      ;dx2http://schemas.devexpress.com/winfx/2008/xaml/core  5      <dxb2http://schemas.devexpress.com/winfx/2008/xaml/bars  5      H
 DBusyIndicator, Version=2.1.0.0, Culture=neutral, PublicKeyToken=nullBbusyIndicator.https://github.com/moh3ngolshani/BusyIndicator 
 5      ^ ZCaliburn.Micro.Platform, Version=4.0.0.0, Culture=neutral, PublicKeyToken=8e5891231f2ed21f(calhttp://www.caliburnproject.org  5      X TXceed.Wpf.Toolkit, Version=4.6.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit  5        � 	Resources  5          BooleanToVisibilityConverter+   %NetErp.Helpers.SimpleBooleanConverter  SimpleBooleanConverter�   �NetErp.Helpers.BooleanConverter`1[[System.Boolean, System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]   True   False/   )NetErp.Helpers.CheckBoxForeColorConverter  CheckBoxForeColorConverter!   NetErp.Helpers.BindingProxy  DataContextProxy   Data��%�   &	        &	      &	 B     &	 U     �� 5   
   6'     5   
   $ False��6B   $
 True��6O     5   
   6'     5   
    62   �� 5      .��5       
 BusyIndicator.BusyMask  -   5          Name$ BusyIndicator��5         IndicatorType$ BouncingDot=�5         BusyContent$ Please wait...��5         BusyContentMargin$ 0,20,0,0q�5         IsBusyAtStartup$ False��5      	   IsBusy	 5      �� 
 �� Path$
 IsBusy� �� Mode$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=�.��5   
   �  � RowDefinitions 5       �� 5!      $
��auto �6    �� 5"      $��* �6    .+�5%      )  #DevExpress.Xpf.Ribbon.RibbonControl  $��0��6    $��3��60      IsSimplified$ False��6D      AllowSimplifiedRibbon$
 True��5&         ShowApplicationButton$ False��69      ToolbarShowMode$
 Hide=�6W      RibbonStyle$ 
Office2010=�5'      %   ToolbarShowCustomizationButton$ False��65      RibbonTitleBarVisibility$ 	Collapsed=�6\   $��Center=�6�      Items. 5)      &   DevExpress.Xpf.Ribbon.RibbonPage     Caption)  #DevExpress.Xpf.Core.ObjectConverter$ General 6!      Groups. 5+      +	  %DevExpress.Xpf.Ribbon.RibbonPageGroup	   	  Caption$ Acciones��6*    	  Items. 5,      '
  !DevExpress.Xpf.Bars.BarButtonItem
  !  DevExpress.Xpf.Bars.BarItem   Content$ Nuevo 5.   ,      RibbonStyle$ Large=�5/   ,      Hint$  Crear un nuevo comprobante 51   ,      Glyph 5,   ,   *  $DevExpress.Xpf.Core.DXImageExtension     Image1  +DevExpress.Xpf.Core.Native.DXImageConverter$ AddFile_16x16.png    
LargeGlyph 5-   ,     $ AddFile_32x32.png   �� 	IsEnabled  50   ,   �� $
 IsBusy�$ OneWay=�$ PropertyChanged=�! �� 	Converter! >  8DevExpress.Mvvm.UI.BooleanToVisibilityConverterExtension  "   Inverse$
" True��2  ,DevExpress.Mvvm.UI.Interactivity.Interaction#   	Behaviors# 52      '  !DevExpress.Mvvm.UI.EventToCommand  53   "   �  �DevExpress.Mvvm.UI.Interactivity.EventTriggerBase`1[[System.Windows.DependencyObject, WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]$   	EventName$$ 	ItemClick��6n   +  %DevExpress.Mvvm.UI.EventToCommandBase%   Command% 68   �� $"
 CreateAccountingEntryCommand�6n   55      *  $DevExpress.Xpf.Bars.BarItemSeparator  56      
  57      $ Eliminar 6,   &   Description$& Eliminar 5:   ,   $ Eliminar comprobantes 5;   ,   $ Large=�5=   ,    58   ,     $ DeleteList_16x16.png  59   ,     $ DeleteList_32x32.png   5<   ,   �� $
 CanDeleteEntry�$ OneWay=�$ PropertyChanged=�# 5>        5?   "   $$ 	ItemClick��6o   % 68   �� $#
 DeleteAccountingEntityCommand�6o   5A      .  (DevExpress.Xpf.Bars.BarItemLinkSeparator  5B      5C      5D      5E      �� 5G      $
��auto��6   $
��auto��6&   $��1��64   $��4q�6A   .@�5H      �� $��10q�6   ' �� 
IsSelected' 6'   �� $
 IsSelectedTab1�$ TwoWay=�$ PropertyChanged=���5I      �� 5J      $��Buscar Comprobante��6$   $��15(�6>   .��5L      � ( � ColumnDefinitions( 5M      �� 5N      $
��1.2* �6/   �� 5O      $
��7.6* �6/   �� 5P      $
��1.2* �6/    5R      �� 5S      $
��auto �6,   �� 5T      $
��auto �6,   �� 5U      $
��auto �6,   �� 5V      $
��auto �6,   �� 5W      $
��auto �6,   �� 5X      $
��auto �6,   �� 5Y      $��* �6,   �� 5Z      $
��auto �6,   .+�5]      �� $��0��6#   $��0��60   $��Libro contable��5^   #   $��Center=�5_   #   $��4q�5`   #   $��3q�6.   $��2��6:   $��15(�5a   #   ��5b   #   �� $
 SearchOnAccountingBook�$ TwoWay=�$ PropertyChanged=�#! [ ) d� 	IsChecked) 5c   #   �� $
 SearchOnAccountingBook�$ TwoWay=�$ PropertyChanged=�6�   �� 5d      $��0��6#   $��1��60   $��
4, 6, 4, 4q�5e   #   $��3q�67   * �� DisplayMemberPath$
* Name��5h   #   + �� SelectedValuePath$+ Id��5i   #   $��Center=�5j   #   y�5f   #   �� $
 SearchOnAccountingBook�$ TwoWay=�$ PropertyChanged=�#! [  ��5g   #   �� $
 Context.AccountingBooks�$ TwoWay=�$ PropertyChanged=�, �� SelectedValue, 5k   #   �� $
 SelectedAccountingBookId�$ TwoWay=�$ PropertyChanged=��� 5m      $��1��6#   $��0��60   $��Centro de costo��5n   #   $��Center=�5o   #   $��4q�5p   #   $��3q�6.   $��2��6:   $��15(�5q   #   ��5r   #   �� $
 SearchOnCostCenter�$ TwoWay=�$ PropertyChanged=�#! [ ) 5s   #   �� $
 SearchOnCostCenter�$ TwoWay=�$ PropertyChanged=��� 5t      $��1��6#   $��1��60   $��4q�5u   #   $��3q�6.   $
* Name��5v   #   $+ Id��5w   #   $��Center=�5x   #   , 5y   #   �� $
 SelectedCostCenterId�$ TwoWay=�$ PropertyChanged=�y�5z   #   �� $
 SearchOnCostCenter�$ TwoWay=�$ PropertyChanged=�#! [  ��5{   #   �� $
 Context.CostCenters�$ TwoWay=�$ PropertyChanged=��� 5}      $��2��6#   $��0��60   $��Fuente contable��5~   #   $��Center=�5   #   $��4q�5�   #   $��3q�6.   $��2��6:   $��15(�5�   #   ��5�   #   �� $
 SearchOnAccountingSource�$ TwoWay=�$ PropertyChanged=�#! [ ) 5�   #   �� $
 SearchOnAccountingSource�$ TwoWay=�$ PropertyChanged=��� 5�      $��2��6#   $��1��60   $��4q�5�   #   $��3q�6.   $
* Name��5�   #   $+ Id��5�   #   $��Center=�5�   #   , 5�   #   �� $ 
 SelectedAccountingSourceId�$ TwoWay=�$ PropertyChanged=�y�5�   #   �� $
 SearchOnAccountingSource�$ TwoWay=�$ PropertyChanged=�#! [  ��5�   #   �� $
 Context.AccountingSources�$ TwoWay=�$ PropertyChanged=��� 5�      $��3��6#   $��0��60   $��Fecha��5�   #   $��Center=�5�   #   $��Center=�5�   #   $��4q�5�   #   $��3q�6.   $��2��6:   $��15(�5�   #   ��5�   #   �� $
 SearchOnDate�$ TwoWay=�$ PropertyChanged=�#! [ ) 5�   #   �� $
 SearchOnDate�$ TwoWay=�$ PropertyChanged=��� 5�      - �� Orientation$- 
Horizontal=�6%   $��3��5�   %   $��1��62   $��4q�5�   %   y�5�   %   �� $
 SearchOnDate�$ TwoWay=�$ PropertyChanged=�#! [  .+�5�      �� $��70��6'   $��0 0 8 0q�62   $��3q�6C   $* Value��5�   '   $	+ Key��5�   '   $��Center=�5�   '   , 5�   '   �� $
 SelectedDateFilterOption�$ TwoWay=�$ PropertyChanged=���5�   '   �� $
 DateFilterOptions�$ OneWay=�$ PropertyChanged=�(  "System.Windows.Controls.DatePicker  5�      $	��105��6)   $��0 0 8 0q�5�   )   $��3q�6:   .   SelectedDate. 5�   )   �� $
 StartDateFilter�$ TwoWay=�$ PropertyChanged=�  5�      $	��105��6)   $��3q�65   . 5�   )   �� $
 EndDateFilter�$ TwoWay=�$ PropertyChanged=�y�5�   )   �� $
 IsDateRange�$ OneWay=�$ PropertyChanged=�#! [  5�      �� 5�      $��4��6#   $��0��60   $��Número de documento��5�   #   $��Center=�5�   #   $��4q�5�   #   $��3q�6.   $��2��6:   $��15(�5�   #   ��5�   #   �� $
 SearchOnDocumentNumber�$ TwoWay=�$ PropertyChanged=�#! [ ) 5�   #   �� $
 SearchOnDocumentNumber�$ TwoWay=�$ PropertyChanged=��� 5�      -   5        / ��Name$/ DocumentNumber��5�   "   $��4��5�   "   $��1��6/   $	��120��5�   "   $
��Left=�5�   "   $��Center=�5�   "   0 �� 	MaxLength$0 9��5�   "   $��4q�5�   "   $��3q�6-   ��5�   "   �� $
 DocumentNumber�$ TwoWay=�$ PropertyChanged=�y�5�   "   �� $
 SearchOnDocumentNumber�$ TwoWay=�$ PropertyChanged=�#! [  �� 5�      $��5��6#   $��0��60   $��Tercero��5�   #   $��Center=�5�   #   $��4q�5�   #   $��3q�6.   $��2��6:   $��15(�5�   #   ��5�   #   �� $
 SearchOnAccountingEntity�$ TwoWay=�$ PropertyChanged=�#! [ ) 5�   #   �� $
 SearchOnAccountingEntity�$ TwoWay=�$ PropertyChanged=�� 5�      $��5��6   $��1��6,   $��4q�5�      y�5�      �� $
 SearchOnAccountingEntity�$ TwoWay=�$ PropertyChanged=�#! [  ( 5�      �� 5�   "   $��2* �63   �� 5�   "   $
��0.3* �63    5�      �� 5�   "   $
��auto �60   .+�5�      �� -   5        1 ��Name$ 1 SelectedAccountingEntityId��5�   '   $��0��5�   '   $��0��64   $��0 0 8 0q�6D   $��3q�6U   $��Center=�5�   '     itemTemplateSelector#���  5�   '   $+ Id��5�   '   y�5�   '   �� $-
 'IsFilterSearchAccountinEntityOnEditMode�$ TwoWay=�$ PropertyChanged=�#! [ , 5�   '   �� $ 
 SelectedAccountingEntityId�$ TwoWay=�$ PropertyChanged=���5�   '   �� $%
 AccountingEntitiesSearchResults�$ TwoWay=�$ PropertyChanged=�  5�   "     selectedTemplate  dropDownTemplate1   +NetErp.Helpers.ComboBoxItemTemplateSelector2   SelectedTemplate3   DropDownTemplate��%J  &	       &	 j     &	     7  7  �� 5�   &   .R�5�   *   �� ��64   �� $
 
SearchName�$ TwoWay=�$ PropertyChanged=�5�   '   �� 5�   &   .R�5�   *   �� $- Vertical=�65   .+�5�   .   �� ��68   �� $
 
SearchName�$ TwoWay=�$ PropertyChanged=��� 5�   .   $- 
Horizontal=�69   .+�5�   2   �� 
�������$��12(�6N   Nit. �� 5�   C   
�������$��12(�5�   O   ��5�   M   �� $/
 )IdentificationNumberWithVerificationDigit�$ OneWay=�$ PropertyChanged=�5�   O   5�   7   5�   /   5�   +   5�   '     5�   &   82   83  5�   -   5�   #   ��5�   "   �� 5�   &   .R�5�   *   $� 5�   '   5�      (  "Xceed.Wpf.Toolkit.WatermarkTextBox  5�      -   5        4  Name$"4 FilterSearchAccountingEntity��5�   4   $��0��5�   4   $��0��6A   $��0 0 8 0q�6Q   $��3q�6b   5 �� CharacterCasing$5 Upper=�5�   4   6   	Watermark$A6 ;Digite por lo menos 3 caracteres y presiona el boton Buscar��5�   4   7   KeepWatermarkOnGotFocus$
7 True��5�   4   $��13(�5�   4   $
��auto��5�   4   $��Center=�5�   4   ��5�   4   �� $"
 FilterSearchAccountingEntity�$ TwoWay=�$ PropertyChanged=�y�5�   4   �� $-
 'IsFilterSearchAccountinEntityOnEditMode�$ TwoWay=�$ PropertyChanged=�#! [  �� 5�      -   5        8 ��Name$$8 SearchForAccountingEntityMatch��5�   %   $��0��5�   %   $��1��62   $��3q�5�   %   ��5�   "   �� 5�   &   $- 
Horizontal=�61   .+�5�   *   �� $��0 0 5 0q�60   $��16��6A   ��5�   .   �� 5�   2   9 �� 
TargetType"9 ��68   : �� Triggers: 5�   6   P� 5�   :   ; P� Property; � {�< P� Value$< False��6W   . �5�   >   �� = �� Property= �@J�Opacity> �� Value$	> 0.5Q�6X   5�   ;   ^� 5   :   ? ^� 
Conditions? 5  >   �� 5  B   @ �� Binding@ 6L   �� *)'IsFilterSearchAccountinEntityOnEditMode+A �� ValueA true6�   .0�5  >   �� = � ��$]> Wpack://application:,,,/DevExpress.Images.v23.2;component/Images/Print/Preview_16x16.png��6W   5  ;   ^� 5  :   ? 5  >   �� 5  B   @ 6L   �� *)'IsFilterSearchAccountinEntityOnEditMode+	A false6�   .0�5
  >   �� = � ��$\> Vpack://application:,,,/DevExpress.Images.v23.2;component/Images/History/Undo_16x16.png��6W   5  ;   5  3   5  +   �� 5  *   $��0 4 0 4q�5  4   $��Center=�6E   ��5  4   �� $
 FilterSearchButtonInfo�$ OneWay=�$ PropertyChanged=�5  E   5  +   5     5     �� 5     -   5        $8 SearchAccountingEntries��5  [   $��5��6!   $��2��6.   $��4 4 0 4q�6>   $��2q�6O   ��5     �� 5  "   $- 
Horizontal=�6-   $��Center=�6F   .+�5  &   �� $w��qpack://application:,,,/DevExpress.Images.v23.2;component/Images/Spreadsheet/FunctionsLookupAndReference_16x16.png��6,   $��0 0 4 0q�6�   $��16��6�   �� 5  &   $��BUSCAR COMPROBANTE��60   $��Center=�6J   5  #   5     &   System.Windows.Controls.DataGrid  5      $��6��6#   $��0��60   $��3��5!  #   $��0 8 0 0q�5"  #   B   AutoGenerateColumns$B False��5#  #   C   VerticalGridLinesBrush
C �����D   HorizontalGridLinesBrush
D �����$��0q�5&  #   E   CanUserAddRows$E False��5'  #   F   GridLinesVisibility$F 
Horizontal=�5(  #   G   SelectionMode$G Single=�5)  #   H   SelectionUnit$H FullRow=�5*  #   I   MinRowHeight$I 30Q�5+  #   J   AlternatingRowBackground
J �����
������ K   CanUserDeleteRows$K False��50  #   ��5-  #   �� $
 AccountingEntriesMaster�$ TwoWay=�$ PropertyChanged=�L �� SelectedItemL 5.  #   �� $
 SelectedAccountingEntry�$ TwoWay=�$ PropertyChanged=�M   	CellStyleM 51     �� 52  "   *  $System.Windows.Controls.DataGridCell"9  6(   .�53  &   �� = � ��$> Center=�6S   �� 54  &   = � ��$> Center=�6Q   55  #   N   ColumnsN 57     4  .System.Windows.Controls.DataGridTemplateColumn  59  "   ,  &System.Windows.Controls.DataGridColumnO   Width5  /System.Windows.Controls.DataGridLengthConverter$O 30 69   P   CellTemplateP 5:  &   �� 5;  *   .R�5<  .   ��   Caliburn.Micro.MessageQ   Attach$SQ M[Event Checked] = [Action OnChecked];[Event Unchecked] = [Action OnUnchecked]��67   $��Center=�5>  7   $��Center=�6T   ) 5=  7   �� *	IsChecked+$ TwoWay=�$ PropertyChanged=�5?  /   5@  +   5B  #   0  *System.Windows.Controls.DataGridTextColumn  5E  "   R   Header$R Fecha Comp.��65   S   FontSize$S 14Q�5G  5   $
O auto 5H  5   1  +System.Windows.Controls.DataGridBoundColumnT   BindingT 5F  5   �� $
 DocumentDate�$ OneWay=�$ PropertyChanged=�U �� StringFormat$U {0:dd/MM/yyyy}��V   HeaderStyleV 5I  &   �� 5J  *   =   7System.Windows.Controls.Primitives.DataGridColumnHeader"9   60   .�5K  .   �� = � ��$> Center=�6[   5L  +   W   	CellStyleW 5N  &   �� 5O  *   "9  60   .�5P  .   �� = � ��$> 5 0 5 0q�6H   �� 5Q  .   = � ��> 5R  2   �� 5S  6   X �� 
TargetType"X  6F   .R�5T  :   �� #��z��6A   Y �� Padding#Y z��6k   ._�5U  >   �� $��Center=�6O   $��Center=�6j   5V  ;   5W  7   5Y  /   : 5Z  .   �� 5[  2   Z �� BindingZ 6>   �� *State+[ �� Value[ X6X   .a�5\  6   �� = � ��
> �  ��6S   5]  3   �� 5^  2   Z 6>   �� *State+[ A6X   .a�5_  6   �� = � ��
> �  ��6S   5`  3   5b  +   5d  #     5f  "   $R Fuente contable��65   $
O auto 5g  5   $S 14Q�5h  5   T 5i  5   �� $
 AccountingSource.Name�$ OneWay=�$ PropertyChanged=�V 5j  &   �� 5k  *   "9   60   .�5l  .   �� = � ��$> Center=�6[   5m  +   W 5o  &   �� 5p  *   "9  60   .�5q  .   �� = � ��$> 5 0 5 0q�6H   �� 5r  .   = � ��> 5s  2   �� 5t  6   "X  6F   .R�5u  :   �� #��z��6A   #Y z��6k   ._�5v  >   �� $
��Left=�6O   $��Center=�6j   5w  ;   5x  7   5z  /   : 5{  .   �� 5|  2   Z 6>   �� *State+[ X6X   .a�5}  6   �� = � ��
> �  ��6S   5~  3   �� 5  2   Z 6>   �� *State+[ A6X   .a�5�  6   �� = � ��
> �  ��6S   5�  3   5�  +   5�  #     5�  "   $R Número��65   $
O auto 5�  5   $S 14Q�5�  5   T 5�  5   �� $
 DocumentNumber�$ OneWay=�$ PropertyChanged=�V 5�  &   �� 5�  *   "9   60   .�5�  .   �� = � ��$> Center=�6[   5�  +   W 5�  &   �� 5�  *   "9  60   .�5�  .   �� = � ��$> 5 0 5 0q�6H   �� 5�  .   = � ��> 5�  2   �� 5�  6   "X  6F   .R�5�  :   �� #��z��6A   #Y z��6k   ._�5�  >   �� $��Center=�6O   $��Center=�6l   5�  ;   5�  7   5�  /   : 5�  .   �� 5�  2   Z 6>   �� *State+[ X6X   .a�5�  6   �� = � ��
> �  ��6S   5�  3   �� 5�  2   Z 6>   �� *State+[ A6X   .a�5�  6   �� = � ��
> �  ��6S   5�  3   5�  +   5�  #     5�  "   $"R Descripción del comprobante��65   $O * 5�  5   $S 14Q�5�  5   T 5�  5   �� $
 Description�$ OneWay=�$ PropertyChanged=�V 5�  &   �� 5�  *   "9   60   .�5�  .   �� = � ��$> Center=�6[   5�  +   W 5�  &   �� 5�  *   "9  60   .�5�  .   �� = � ��$> 5 0 5 0q�6H   �� 5�  .   = � ��> 5�  2   �� 5�  6   "X  6F   .R�5�  :   �� #��z��6A   #Y z��6k   ._�5�  >   �� $
��Left=�6O   $��Center=�6j   5�  ;   5�  7   5�  /   : 5�  .   �� 5�  2   Z 6>   �� *State+[ X6X   .a�5�  6   �� = � ��
> �  ��6S   5�  3   �� 5�  2   Z 6>   �� *State+[ A6X   .a�5�  6   �� = � ��
> �  ��6S   5�  3   5�  +   5�  #     5�  "   $R 
Creado Por��65   $
O auto 5�  5   $S 14Q�5�  5   T 5�  5   �� $
 	CreatedBy�$ OneWay=�$ PropertyChanged=�V 5�  &   �� 5�  *   "9   60   .�5�  .   �� = � ��$> Center=�6[   5�  +   W 5�  &   �� 5�  *   "9  60   .�5�  .   �� = � ��$> 5 0 5 0q�6H   �� 5�  .   = � ��> 5�  2   �� 5�  6   "X  6F   .R�5�  :   �� #��z��6A   #Y z��6k   ._�5�  >   �� $
��Left=�6O   $��Center=�6j   5�  ;   5�  7   5�  /   : 5�  .   �� 5�  2   Z 6>   �� *State+[ X6X   .a�5�  6   �� = � ��
> �  ��6S   5�  3   �� 5�  2   Z 6>   �� *State+[ A6X   .a�5�  6   �� = � ��
> �  ��6S   5�  3   5�  +   5�  #     5�  "   $R Fecha Creación��65   $S 14Q�5�  5   $
O auto 5�  5   T 5�  5   �� $
 	CreatedAt�$ OneWay=�$ PropertyChanged=�$U {0:dd/MM/yyyy hh:mm tt}��V 5�  &   �� 5�  *   "9   60   .�5�  .   �� = � ��$> Center=�6[   5�  +   W 5�  &   �� 5�  *   "9  60   .�5�  .   �� = � ��$> 5 0 5 0q�6H   �� 5�  .   = � ��> 5�  2   �� 5�  6   "X  6F   .R�5�  :   �� #��z��6A   #Y z��6k   ._�5�  >   �� $��Center=�6O   $��Center=�6l   5�  ;   5�  7   5�  /   : 5�  .   �� 5   2   Z 6>   �� *State+[ X6X   .a�5  6   �� = � ��
> �  ��6S   5  3   �� 5  2   Z 6>   �� *State+[ A6X   .a�5  6   �� = � ��
> �  ��6S   5  3   5  +   5	  #     5  "   $R Centro de costo��65   $
O auto 5  5   $S 14Q�5  5   T 5  5   �� $
 CostCenter.Name�$ OneWay=�$ PropertyChanged=�V 5  &   �� 5  *   "9   60   .�5  .   �� = � ��$> Center=�6[   5  +   W 5  &   �� 5  *   "9  60   .�5  .   �� = � ��$> 5 0 5 0q�6H   �� 5  .   = � ��> 5  2   �� 5  6   "X  6F   .R�5  :   �� #��z��6A   #Y z��6k   ._�5  >   �� $
��Left=�6O   $��Center=�6j   5  ;   5  7   5  /   : 5   .   �� 5!  2   Z 6>   �� *State+[ X6X   .a�5"  6   �� = � ��
> �  ��6S   5#  3   �� 5$  2   Z 6>   �� *State+[ A6X   .a�5%  6   �� = � ��
> �  ��6S   5&  3   5(  +   5*  #     5,  "   $R Libro��65   $
O auto 5-  5   T 5.  5   �� $
 AccountingBook.Name�$ OneWay=�$ PropertyChanged=�V 5/  &   �� 50  *   "9   60   .�51  .   �� = � ��$> Center=�6[   52  +   W 54  &   �� 55  *   "9  60   .�56  .   �� = � ��$> 5 0 5 0q�6H   �� 57  .   = � ��> 58  2   �� 59  6   "X  6F   .R�5:  :   �� #��z��6A   #Y z��6k   ._�5;  >   �� $
��Left=�6O   $��Center=�6j   5<  ;   5=  7   5?  /   : 5@  .   �� 5A  2   Z 6>   �� *State+[ X6X   .a�5B  6   �� = � ��
> �  ��6S   5C  3   �� 5D  2   Z 6>   �� *State+[ A6X   .a�5E  6   �� = � ��
> �  ��6S   5F  3   5H  +   5J  #     5L  "   $R Información��65   $	O 300 5N  5   $S 14Q�5O  5   T 5M  5   �� $

 Info�$ OneWay=�$ PropertyChanged=�V 5P  &   �� 5Q  *   "9   60   .�5R  .   �� = � ��$> Center=�6[   5S  +   W 5U  &   �� 5V  *   "9  60   .�5W  .   �� = � ��$> 5 0 5 0q�6H   �� 5X  .   = � ��> 5Y  2   �� 5Z  6   "X  6F   .R�5[  :   �� #��z��6A   #Y z��6k   ._�5\  >   �� $
��Left=�6O   $��Center=�6j   5]  ;   5^  7   5`  /   : 5a  .   �� 5b  2   Z 6>   �� *State+[ X6X   .a�5c  6   �� = � ��
> �  ��6S   5d  3   �� 5e  2   Z 6>   �� *State+[ A6X   .a�5f  6   �� = � ��
> �  ��6S   5g  3   5i  +   \   ElementStyle\ 5k  &   �� 5l  *   "9 ��60   .�5m  .   �� = � ��$
> Wrap=�6W   5n  +   5p  #     5r  "   $O 30 69   P 5s  &   �� 5t  *   .R�5u  .   �� $	��...��65   $��15(�5v  )   $��20��5w  )   $��20��64   $&Q  [Event Click] = [Action ZoomDoc]��5x  )   5y  +   5{  #   5}     $!   NetErp.UserControls.GridPaging!  5     $��7��6(   $��0��65   $��3��6E   $��4q�6Y   $��2��6d   ] !  PageSize] 5�  (   �� $
 PageSize�$ TwoWay=�$ PropertyChanged=�^ !  	PageIndex^ 5�  (   �� $
 	PageIndex�$ TwoWay=�$ PropertyChanged=�_ !  
TotalCount_ 5�  (   �� $
 
TotalCount�$ TwoWay=�$ PropertyChanged=�` !  ResponseTime` 5�  (   �� $
 ResponseTime�$ TwoWay=�$ PropertyChanged=�5�     5�     �� 5�     $��10q�6   ' 6'   �� $
 IsSelectedTab2�$ TwoWay=�$ PropertyChanged=���5�     �� 5�     $��Buscar Borradores��6$   $��15(�6=   .��5�     �  5�     �� 5�     $
��auto �6,   �� 5�     $
��auto �6,   �� 5�     $��* �6,   �� 5�     $
��auto �6,   .+�5�     �� $��¿ Que desea buscar ?��6$   $��0��6@   $��4q�6M   a �� Padding$a 3q�6X   �� 5�     $��1��6"   $��4q�6/   $��3q�6:     5�     $��2��6#   $��4q�60   $B False��5�  #   
C �����
D �����$��0q�5�  #   $E false��5�  #   $G Single=�5�  #   $H FullRow=�5�  #   $F 
Horizontal=�5�  #   $I 30Q�5�  #   
J �����
������ $K False��5�  #   ��5�  #   �� $"
 AccountingEntriesDraftMaster�$ TwoWay=�$ PropertyChanged=�M 5�     �� 5�  "   "9  6(   .�5�  &   �� = � ��$> Center=�6S   �� 5�  &   = � ��$> Center=�6Q   5�  #   N 5�       5�  "   $O 30 69   P 5�  &   �� 5�  *   .R�5�  .   �� $��Center=�67   $��Center=�5�  7   $SQ M[Event Checked] = [Action OnChecked];[Event Unchecked] = [Action OnUnchecked]��5�  7   ) 5�  7   �� *	IsChecked+$ TwoWay=�$ PropertyChanged=�5�  /   5�  +   5�  #     5�  "   $R Fecha��65   $	O 100 5�  5   $S 14Q�5�  5   T 5�  5   �� $
 DocumentDate�$ OneWay=�$ PropertyChanged=�$U {0:dd/MM/yyyy}��V 5�  &   �� 5�  *   "9   60   .�5�  .   �� = � ��$> Center=�6[   �� 5�  .   = � ��$> Center=�6Y   5�  +   W 5�  &   �� 5�  *   "9  60   .�5�  .   �� = � ��$> 5 0 5 0q�6H   �� 5�  .   = � ��> 5�  2   �� 5�  6   "X  6F   .R�5�  :   �� #��z��6A   #Y z��6k   ._�5�  >   �� $��Center=�6O   $��Center=�6j   5�  ;   5�  7   5�  /   5�  +   5�  #     5�  "   $R Fuente contable��65   $	O .8* 5�  5   $S 14Q�5�  5   T 5�  5   �� $
 AccountingSource.Name�$ OneWay=�$ PropertyChanged=�V 5�  &   �� 5�  *   "9   60   .�5�  .   �� = � ��$> Center=�6[   �� 5�  .   = � ��$> Center=�6Y   5�  +   W 5�  &   �� 5�  *   "9  60   .�5�  .   �� = � ��$> 5 0 5 0q�6H   �� 5�  .   = � ��> 5�  2   �� 5�  6   "X  6F   .R�5�  :   �� #��z��6A   #Y z��6k   ._�5�  >   �� $��Center=�6O   $��Center=�6j   5�  ;   5�  7   5�  /   5�  +   5�  #     5�  "   $R Número��65   $O 80 5�  5   $S 14Q�5�  5   T 5�  5   �� $
 DocumentNumber�$ OneWay=�$ PropertyChanged=�V 5�  &   �� 5�  *   "9   60   .�5�  .   �� = � ��$> Center=�6[   �� 5�  .   = � ��$> Center=�6Y   5�  +   W 5�  &   �� 5�  *   "9  60   .�5�  .   �� = � ��$> 5 0 5 0q�6H   �� 5�  .   = � ��> 5�  2   �� 5�  6   "X  6F   .R�5�  :   �� #��z��6A   #Y z��6k   ._�5�  >   �� $��Center=�6O   $��Center=�6j   5�  ;   5�  7   5  /   5  +   5  #     5  "   $R Descripcion��65   $O 1* 5  5   $S 14Q�5	  5   T 5
  5   �� *Description+$ OneWay=�$ PropertyChanged=�V 5  &   �� 5  *   "9   60   .�5  .   �� = � ��$> Center=�6[   �� 5  .   = � ��$> Center=�6Y   5  +   W 5  &   �� 5  *   "9  60   .�5  .   �� = � ��$> 5 0 5 0q�6H   �� 5  .   = � ��> 5  2   �� 5  6   "X  6F   .R�5  :   �� #��z��6A   #Y z��6k   ._�5  >   �� $��Center=�6O   $��Center=�6j   5  ;   5  7   5  /   5  +   5  #     5"  "   $R 
Creado Por��65   $
O auto 5#  5   $S 14Q�5$  5   T 5%  5   �� $
 	CreatedBy�$ OneWay=�$ PropertyChanged=�V 5&  &   �� 5'  *   "9   60   .�5(  .   �� = � ��$> Center=�6[   �� 5)  .   = � ��$> Center=�6Y   5*  +   W 5,  &   �� 5-  *   "9  60   .�5.  .   �� = � ��$> 5 0 5 0q�6H   �� 5/  .   = � ��> 50  2   �� 51  6   "X  6F   .R�52  :   �� #��z��6A   #Y z��6k   ._�53  >   �� $��Center=�6O   $��Center=�6j   54  ;   55  7   57  /   58  +   5:  #     5=  "   $R Fecha Creación��65   $	O 150 5>  5   $S 14Q�5?  5   T 5@  5   �� *	CreatedAt+$ OneWay=�$ PropertyChanged=�$U {0:dd/MM/yyyy hh:mm tt}��V 5A  &   �� 5B  *   "9   60   .�5C  .   �� = � ��$> Center=�6[   �� 5D  .   = � ��$> Center=�6Y   5E  +   W 5G  &   �� 5H  *   "9  60   .�5I  .   �� = � ��$> 5 0 5 0q�6H   �� 5J  .   = � ��> 5K  2   �� 5L  6   "X  6F   .R�5M  :   �� #��z��6A   #Y z��6k   ._�5N  >   �� $��Center=�6O   $��Center=�6j   5O  ;   5P  7   5R  /   5S  +   5U  #     5X  "   $R Centro de costo��65   $	O .7* 5Y  5   $S 14Q�5Z  5   T 5[  5   �� *CostCenter.Name+$ OneWay=�$ PropertyChanged=�V 5\  &   �� 5]  *   "9   60   .�5^  .   �� = � ��$> Center=�6[   �� 5_  .   = � ��$> Center=�6Y   5`  +   W 5b  &   �� 5c  *   "9  60   .�5d  .   �� = � ��$> 5 0 5 0q�6H   �� 5e  .   = � ��> 5f  2   �� 5g  6   "X  6F   .R�5h  :   �� #��z��6A   #Y z��6k   ._�5i  >   �� $��Center=�6O   $��Center=�6j   5j  ;   5k  7   5m  /   5n  +   5p  #     5s  "   $R Libro��65   $
O auto 5t  5   $S 14Q�5u  5   T 5v  5   �� $
 AccountingBook.Name�$ OneWay=�$ PropertyChanged=�V 5w  &   �� 5x  *   "9   60   .�5y  .   �� = � ��$> Center=�6[   �� 5z  .   = � ��$> Center=�6Y   5{  +   W 5}  &   �� 5~  *   "9  60   .�5  .   �� = � ��$> 5 0 5 0q�6H   �� 5�  .   = � ��> 5�  2   �� 5�  6   "X  6F   .R�5�  :   �� #��z��6A   #Y z��6k   ._�5�  >   �� $��Center=�6O   $��Center=�6j   5�  ;   5�  7   5�  /   5�  +   5�  #     5�  "   $O 30 69   P 5�  &   �� 5�  *   .R�5�  .   �� $	��...��65   $��15(�5�  5   $��3q�5�  5   ��5�  5   �� $ 
 Data.EditDraftEntryCommand�b �� Source#b [ ��5�  5   �� 5�  +   5�  #   5�     !  5�     $��3��6(   $��4q�65   c !  ChangedIndexCommandc 5�  (   �� $
 DraftPaginationCommand�^ 5�  (   �� $
 DraftPageIndex�$ TwoWay=�$ PropertyChanged=�] 5�  (   �� $
 DraftPageSize�$ TwoWay=�$ PropertyChanged=�_ 5�  (   �� $
 DraftTotalCount�$ TwoWay=�$ PropertyChanged=�` 5�  (   �� $
 DraftResponseTime�$ OneWay=�$ PropertyChanged=�5�     5�     5�     5�     5�     5�     