   M S B A M L   `   `   `  ����A  =NetErp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:NetErp.HelpersNetErp.Helpers  5      yAclr-namespace:NetErp.Inventory.CatalogItems.TabControlPages.Views3NetErp.Inventory.CatalogItems.TabControlPages.Views  5      Y1clr-namespace:NetErp.Inventory.CatalogItems.Views#NetErp.Inventory.CatalogItems.Views  5      I    CNetErp.Inventory.CatalogItems.TabControlPages.Views.RelatedProducts   5      a ]DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aW SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      ?dxe5http://schemas.devexpress.com/winfx/2008/xaml/editors  5      :d2http://schemas.microsoft.com/expression/blend/2008  5      $hclr-namespace:NetErp.Helpers  5      ^ ZCaliburn.Micro.Platform, Version=4.0.0.0, Culture=neutral, PublicKeyToken=8e5891231f2ed21f"calhttp://caliburnmicro.com  5      ;dx2http://schemas.devexpress.com/winfx/2008/xaml/core  5	      a	 ]DevExpress.Xpf.Grid.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49af
 bDevExpress.Xpf.Grid.v23.2.Core, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49al hDevExpress.Xpf.Grid.v23.2.Extensions, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a@dxg2http://schemas.devexpress.com/winfx/2008/xaml/grid 	 
  5
      ] YDevExpress.Mvvm.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aAdxmvvm2http://schemas.devexpress.com/winfx/2008/xaml/mvvm   5      OlocalAclr-namespace:NetErp.Inventory.CatalogItems.TabControlPages.Views   5      ?views1clr-namespace:NetErp.Inventory.CatalogItems.Views   5        � 	Resources  5      !   NetErp.Helpers.BindingProxy   DataContextProxy   Data��%&   &	          5   
    62   �� 5      .��5      �  � RowDefinitions 5   
   �� 5      $��0.14* �6   �� 5      $��* �6   .+�5   
   � $��0��6    � ColumnDefinitions 5      �� 5      $
��0.3* �6#   �� 5      $
��1.5* �6#   �� 5      $
��0.5* �6#   �� 5      $
��0.5* �6#   �� 5      $
��0.3* �6#   �� 5       $
��0.3* �6#   .+�5#      �� $��5 20 5 5q�6   $��0��6'   $
��auto��67   $
��auto��6�   ��6D   ��  �� Path$$ Data.OpenSearchRelatedProducts� �� Source# [  .��5$      �� $w��qpack://application:,,,/DevExpress.Images.v23.2;component/Images/Spreadsheet/FunctionsLookupAndReference_16x16.png��6   $
��None=�6�   ��5%      �� 5&       �� 
TargetType" ��6     �� Triggers 5'      P� 5(   "    P� Property � {�	 P� Value$	 False��6?   . �5)   &   �� 
 �� Property
 �@J�Opacity �� Value$ .5Q�6@   5*   #   5,      5.      5/      �� 51       �� Orientation$ Vertical=�6   $��1��60   $��5q�6@   .+�52      �� $$��Nombre del producto o servicio��6   �� 53      -   5         ��Name$ RelatedProductName��53   �   ${�False��6   $��0 5 0 0q�6,    �� CharacterCasing$ Upper=�54   G   ��53   =   �� *RelatedProductName+ �� Mode$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=�54   G   55      �� 56      $ Vertical=�6   $��2��60   $��5q�6@   .+�57      �� $��
Referencia��6   �� 58      ${�False��6   $��0 5 0 0q�6,   $ Upper=�6�   ��6=   �� *RelatedProductReference+$ TwoWay=�$ PropertyChanged=�6�   59      �� 5:      $ Vertical=�6   $��3��60   $��5q�6@   .+�5;      �� $��Código��6   �� 5<      ${�False��6   $��0 5 0 0q�6,   $ Upper=�6�   ��6=   �� *RelatedProductCode+$ TwoWay=�$ PropertyChanged=�6�   5=      �� 5>      $ Vertical=�6   $��4��60   $��5q�6@   .+�5?      �� $��Cantidad��6   %  DevExpress.Xpf.Editors.SpinEdit  5@      -   5          Name$ RelatedProductQuantity��5A      $��0 5 0 0q�5@         MinValue 060   {�6=   �� * RelatedProductQuantityIsEnable+$ TwoWay=�$ PropertyChanged=�   IsFloatValue 5A   ?   �� *RelatedProductAllowFraction+$ TwoWay=�$ PropertyChanged=�5C      �� 5E      $��5 20 5 5q�6   $��5��6'   $
��auto��67   ��6D   �� $# Data.AddRelatedProductCommand�# [  .��5F      �� $[��Upack://application:,,,/DevExpress.Images.v23.2;component/Images/Actions/Add_16x16.png��6   $
��None=�6w   ��5G      �� 5H      " ��6     5I      P� 5J   "    � {�$	 False��6?   . �5K   &   �� 
 �@J�Opacity$ .5Q�6@   5L   #   5N      5P      5Q      5S      % 	 DevExpress.Xpf.Grid.GridControl  5U   
   $��0 8 0 0q�5V   
   ) 
 #DevExpress.Xpf.Grid.DataControlBase#   EnableSmartColumnsGeneration$
 True��5W   
      AutoGenerateColumns$
 None=�5Y   
   $��1��6%      ItemsSource 5X   
   �� *RelatedProducts+$ TwoWay=�$ PropertyChanged=�   SelectedItem 5Z   
   �� *SelectedRelatedProduct+$ TwoWay=�$ PropertyChanged=�   View 5[      # 	 DevExpress.Xpf.Grid.TableView  5\         AllowCascadeUpdate$ False��6!   & 
  DevExpress.Xpf.Grid.DataViewBase"   EnableSelectedRowAppearance$
 True��5]         AllowEditing$ False��5^      & 	  DevExpress.Xpf.Grid.GridViewBase   ShowGroupPanel$ False��5_         AlternateRowBackground
 �����64      Columns. 5b      $ 	 DevExpress.Xpf.Grid.GridColumn  $	 
 DevExpress.Xpf.Grid.BaseColumn 	  Header)
  #DevExpress.Xpf.Core.ObjectConverter$ Nombre del producto
 5c        	  Width2 
 ,DevExpress.Xpf.Grid.GridColumnWidthConverter$  * 5d      $ 
 DevExpress.Xpf.Grid.ColumnBase!   Binding! 5e      �� *	Item.Name+$ TwoWay=�$ PropertyChanged=�  5g      $ 
Referencia
 5h      $  * 5i      ! 5j      �� *Item.Reference+$ OneWay=�$ PropertyChanged=�  5m      $ Código
 5n      $  * 5o      ! 5p      �� *	Item.Code+$ TwoWay=�$ PropertyChanged=�  5r      $ Cantidad
 5s      $  * 5t      ! 5u      �� *
Quantity+$ TwoWay=�$ PropertyChanged=�  5w      $ Unidad de medida
 5x      $  * 5y      ! 5z      �� *Item.MeasurementUnit.Name+$ TwoWay=�$ PropertyChanged=�  5|      $
  auto 6   "   CellTemplate" 5}      �� 5~      .R�5      �� $��0q�5�      $��3q�5�      $��3q�5�      $
��auto��5�      $
��auto��6+   ��69   �� $&  Data.DeleteRelatedProductCommand�# [  ��5�      �� 5�   "   $
��None=�6_   ��6(   *  $DevExpress.Xpf.Core.DXImageExtension  * Images/Actions/Trash_16x16.png+6_   5�      5�      5�      5�      5�      5�      