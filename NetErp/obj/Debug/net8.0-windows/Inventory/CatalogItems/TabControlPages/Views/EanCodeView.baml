   M S B A M L   `   `   `  ����A  =NetErp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:NetErp.HelpersNetErp.Helpers  5      yAclr-namespace:NetErp.Inventory.CatalogItems.TabControlPages.Views3NetErp.Inventory.CatalogItems.TabControlPages.Views  5	      E    ?NetErp.Inventory.CatalogItems.TabControlPages.Views.EanCodeView   5      a ]DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aW SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      *hclr-namespace:NetErp.Helpers     5      a ]DevExpress.Xpf.Grid.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49af	 bDevExpress.Xpf.Grid.v23.2.Core, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49al
 hDevExpress.Xpf.Grid.v23.2.Extensions, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a@dxg2http://schemas.devexpress.com/winfx/2008/xaml/grid  	 
 5      Hmc;http://schemas.openxmlformats.org/markup-compatibility/2006  	 
 5      >d2http://schemas.microsoft.com/expression/blend/2008  	 
 5      ;dx2http://schemas.devexpress.com/winfx/2008/xaml/core  5      MlocalAclr-namespace:NetErp.Inventory.CatalogItems.TabControlPages.Views  5	        � 	Resources  5      !   NetErp.Helpers.BindingProxy   DataContextProxy   Data��%&   &	          5   
    62   �� 5      .��5      �  � RowDefinitions 5   
   �� 5      $��0.06* �6   �� 5      .+�5   
   �� .+�5      �  � ColumnDefinitions 5      �� 5      $��* �6'   �� 5      $��0.05* �6'   .+�5      �� $��0��6    �� CharacterCasing$ Upper=�6u   ��6*   �� *	EanCode+ �� Mode$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=�6u   �� 5      $��5 0 0 0 q�6   $��1��6+   $
��auto��6;   ��6H   ��  �� Path$ Data.AddEanCodeCommand� �� Source# [  .��5      �� $[��Upack://application:,,,/DevExpress.Images.v23.2;component/Images/Actions/Add_16x16.png��6   $
��None=�6{   ��5      �� 5      	 �� 
TargetType"	 ��6$   
 �� Triggers
 5    "   P� 5!   &    P� Property � {� P� Value$ False��6C   . �5"   *   ��  �� Property �@J�Opacity �� Value$ .5Q�6D   5#   '   5%      5'      5(      5)      5*      %  DevExpress.Xpf.Grid.GridControl  5+   
   $��0 8 0 0q�5,      ) 	 #DevExpress.Xpf.Grid.DataControlBase#   EnableSmartColumnsGeneration$
 True��5-         AutoGenerateColumns$
 None=�5/      $��1��6-      ItemsSource 5.      �� *
EanCodes+$ TwoWay=�$ PropertyChanged=�   SelectedItem 50      �� *SelectedEanCode+$ TwoWay=�$ PropertyChanged=�   View 51      #  DevExpress.Xpf.Grid.TableView  52         AllowCascadeUpdate$ False��6%   & 	  DevExpress.Xpf.Grid.DataViewBase"   EnableSelectedRowAppearance$
 True��53   %   &   DevExpress.Xpf.Grid.GridViewBase   ShowGroupPanel$ False��54   %      AlternateRowBackground
 �����6<      Columns. 57      $  DevExpress.Xpf.Grid.GridColumn  $ 	 DevExpress.Xpf.Grid.BaseColumn   Header)	  #DevExpress.Xpf.Core.ObjectConverter$ Códigos de barras	 58         Width2
 	 ,DevExpress.Xpf.Grid.GridColumnWidthConverter$ *
 59      $ 	 DevExpress.Xpf.Grid.ColumnBase   Binding 5:      �� *	EanCode+$ OneWay=�$ PropertyChanged=�  5<      $
 auto
 6      CellTemplate 5=      �� 5>      .R�5?      �� $��0q�5@      $��3q�5A      $��3q�5B      $
��auto��5C      $
��auto��6'   ��65   �� $ Data.DeleteEanCodeCommand�# [  ��5D      �� 5E   "   $
��None=�6_   ��6(   *  $DevExpress.Xpf.Core.DXImageExtension  * Images/Actions/Trash_16x16.png+6_   5G      5H      5J      5K      5L      5M      