   M S B A M L   `   `   `  ����A  =NetErp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:NetErp.HelpersNetErp.Helpers  5      yAclr-namespace:NetErp.Inventory.CatalogItems.TabControlPages.Views3NetErp.Inventory.CatalogItems.TabControlPages.Views  5      C    =NetErp.Inventory.CatalogItems.TabControlPages.Views.BasicView   5      a ]DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aW SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5      .hclr-namespace:NetErp.Helpers       5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      ] YDevExpress.Mvvm.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aAdxmvvm2http://schemas.devexpress.com/winfx/2008/xaml/mvvm   5      Fmc;http://schemas.openxmlformats.org/markup-compatibility/2006   5      <d2http://schemas.microsoft.com/expression/blend/2008   5      _	 [Microsoft.Xaml.Behaviors, Version=1.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a:behavior+http://schemas.microsoft.com/xaml/behaviors 	 5      ;dx2http://schemas.devexpress.com/winfx/2008/xaml/core  5	      j
 fDevExpress.Xpf.LayoutControl.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aFdxlc;http://schemas.devexpress.com/winfx/2008/xaml/layoutcontrol 
 5
      ?dxe5http://schemas.devexpress.com/winfx/2008/xaml/editors  5      MlocalAclr-namespace:NetErp.Inventory.CatalogItems.TabControlPages.Views  5        � 	Resources  5      !   NetErp.Helpers.BindingProxy   DataContextProxy   Data   BooleanToVisibilityConverter+   %NetErp.Helpers.SimpleBooleanConverter  SimpleBooleanConverter�   �NetErp.Helpers.BooleanConverter`1[[System.Boolean, System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]   True   False)   #NetErp.Helpers.EnumBooleanConverter  EnumBooleanConverter �� Color �� Opacity  ValidationBrush �� 
TargetType �� Triggers P� Property	 P� Value
 �� Property �� Value � DataContext �� RelativeSource �� Path �� DisplayMemberPath��%u  &	        &	      &	 /     &	 ^     &	 q     '�� �     7    5   
    62   �� �� 5   
   6'     5   
   $ False��6B   $
 True��6O     5   
   6!   �� 5   
   $
 Pink��6   $	 0.4Q�6'   65   �� 5   
   " ��6    5      P� 5       �@=�HasError$
	 True��69   . �5      �� 
 �@�ToolTip 5      _� 5       6&   ��  �� *Self+$ PlacementTarget�.��5   "   �� $ ErrorContent��6`   ��6/   �� $ (Validation.Errors)�6`   5      5      �� 5      
 � ��8   63   �� 5       
 �@=�ErrorTemplate 6A   Z� 5!      5#      5$      .��5%      �  � RowDefinitions 5&   
   �� 5'      $��0.08* �6   �� 5(      $
��auto �6   �� 5)      $
��auto �6   �� 5*      $
��auto �6   �� 5+      $
��auto �6   �� 5,      $
��auto �6   �� 5-      $
��auto �6   �� 5.      $
��auto �6   �� 5/      $
��auto �6    � ColumnDefinitions 51   
   �� 52      $
��auto �6   �� 53      $��* �6   .+�56   
   4 
 .DevExpress.Xpf.LayoutControl.TileLayoutControl  $��0��67   $��0��6G   $��Center=�57   !   4 
 .DevExpress.Xpf.LayoutControl.FlowLayoutControl   Orientation$ 
Horizontal=�6>   4 
 .DevExpress.Xpf.LayoutControl.LayoutControlBase   Padding$ 0q�6W      BreakFlowToFit$ False��6c   $��Center=�6z   $��2��6�   ��56   !   Z�    ItemsSource 6T   �� *
ItemImages+ �� Mode$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=�   ItemTemplate 59      �� 5:      .R�5;      ' 
 !DevExpress.Xpf.LayoutControl.Tile     Size$ Small=�6    
�������$��Center=�6T   $��Center=�6o   ,	  &DevExpress.Xpf.Core.ContentControlBase 	  Content 6-   ��  � ContextMenu 5<      �� 5=      .@�5>   "   m� $��Eliminar imagen��6+    m� Command 6D   �� *Data.DeleteImageCommand+ �� Source# [   m� CommandParameter 6�   ��  m� Icon 5?   &   �� 5@   *   ��60   *
  $DevExpress.Xpf.Core.DXImageExtension
  *Images/Edit/Delete_16x16.png+5B   #   5C        	  ContentTemplate  5E      �� 5F      .R�5G   "   � .+�5H   &   �� ��6,   �� *SourceImage+$ PropertyChanged=�5I   #   5J      5L      5M      5O      �� 5Q   
   $��0��6i   $��1��6y   $��2��6�   $��Center=�6�   $��0 5 0 10q�6�   ��6   �� $ Data.AddImageCommand�# [  ��5R      �� 5S      ! �� Orientation$! 
Horizontal=�6   .+�5T      �� $��Agregar imagen��6    $��Center=�66   �� 5U      $[��Upack://application:,,,/DevExpress.Images.v23.2;component/Images/Actions/Add_32x32.png��6   $��Center=�6{   5V      5X      �� 5Z   
   $��Código��6   $��2��6"   $��0��6/   $��Right=�6?   $��0 0 5 0q�6[   $��Center=�6l   y�6�   �� *IsNewRecord+" �� 	Converter" >  8DevExpress.Mvvm.UI.BooleanToVisibilityConverterExtension  #   Inverse$
# True���� 5[   
   $��2��6   $��1��6   $ �� 
IsReadOnly$
$ True��6w   $��0 0 0 5q�6�   % �� CharacterCasing$% Upper=�6�   ��6/   �� *Code+$ TwoWay=�$ PropertyChanged=�y�6�   �� *IsNewRecord+"   $
# True��6�   �� 5]   
   $��Nombre��6   $��3��6"   $��0��6/   $��Right=�6?   $��0 0 5 0q�6[   $��Center=�6l   %  DevExpress.Xpf.Editors.TextEdit  5^   
   -   5        &  Name$
& Name��5^      $��3��6m   $��1��6z   $��0 0 0 5q�6�   '   CharacterCasing$' Upper=�6�   )  #DevExpress.Xpf.Editors.TextEditBase(   Text( 6%   �� *Name+$ TwoWay=�$ PropertyChanged=�2  ,DevExpress.Mvvm.UI.Interactivity.Interaction)   	Behaviors) 5_      &   DevExpress.Mvvm.UI.FocusBehavior  5`      *   PropertyName$* NameIsFocused��6t   �  �DevExpress.Mvvm.UI.Interactivity.EventTriggerBase`1[[System.Windows.Controls.Control, PresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]+   SourceObject+ 6'   �� $
 Data�# [  6t   5b      �� 5d   
   $��
Referencia��6   $��4��6&   $��0��63   $��Right=�6C   $��0 0 5 0q�6_   $��Center=�6p   �� 5e   
   $��4��6   $��1��6   $��0 0 0 5q�6|   $% Upper=�6�   ��6/   �� *	Reference+$ TwoWay=�$ PropertyChanged=�6�   �� 5g   
   $��Unidad de medida��6   $��5��6,   $��0��69   $��Right=�6I   $��0 0 5 0q�6e   $��Center=�6v   �� 5h   
   $��Stretch=�6   $
 Name��5j      $��5��5l      $��1��6"   $��0 0 0 5q�62   ��5i      �� *MeasurementUnits+$ TwoWay=�$ PropertyChanged=�, �� SelectedItem, 5k      �� *SelectedMeasurementUnit+$ TwoWay=�$ PropertyChanged=�5l   2   �� 5o   
   $��Marca��6   $��6��6!   $��0��6.   $��Right=�6>   $��0 0 5 0q�6Z   $��Center=�6k   �� 5p   
   $��Stretch=�6   $
 Name��5q      $��6��5s      $��1��6"   $��0 0 0 5q�62   ��5p   1   �� *Brands+$ TwoWay=�$ PropertyChanged=�, 5r      �� *SelectedBrand+$ TwoWay=�$ PropertyChanged=�5s   2   �� 5w   
   $��Grupo de tallaje��6   $��7��6,   $��0��69   $��Right=�6I   $��0 0 5 0q�6e   $��Center=�6v   �� 5x   
   $��Stretch=�6   $
 Name��5y      $��7��5{      $��1��6&   $��0 0 0 5q�66   ��5x   1   �� *Sizes+$ TwoWay=�$ PropertyChanged=�, 5z      �� *SelectedSize+$ TwoWay=�$ PropertyChanged=�5{   6   �� 5}   
   $��Grupo contable��6   $��8��6*   $��0��67   $��Right=�6G   $��0 0 5 0q�6c   $��Center=�6t   �� 5~   
   $��Stretch=�6   $
 Name��5      $��8��5�      $��1��6&   ��5~   1   �� *AccountingGroups+$ TwoWay=�$ PropertyChanged=�, 5�      �� *SelectedAccountingGroup+$ TwoWay=�$ PropertyChanged=�5�   &   5�      5�      