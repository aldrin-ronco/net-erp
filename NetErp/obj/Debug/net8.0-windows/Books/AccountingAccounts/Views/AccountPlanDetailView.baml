   M S B A M L   `   `   `  ����A  =NetErp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:NetErp.HelpersNetErp.Helpers  5      O Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem 5      g8clr-namespace:NetErp.Books.AccountingAccounts.ViewModels*NetErp.Books.AccountingAccounts.ViewModels  5      7 clr-namespace:NetErp.IoContainerNetErp.IoContainer  5      A    ;NetErp.Books.AccountingAccounts.Views.AccountPlanDetailView   5      a ]DevExpress.Xpf.Core.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aW SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      *hclr-namespace:NetErp.Helpers     5      ^	 ZCaliburn.Micro.Platform, Version=4.0.0.0, Culture=neutral, PublicKeyToken=8e5891231f2ed21f"calhttp://caliburnmicro.com 	 5      X
 TXceed.Wpf.Toolkit, Version=4.6.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit 
 5	      H DBusyIndicator, Version=2.1.0.0, Culture=neutral, PublicKeyToken=nullBbusyIndicator.https://github.com/moh3ngolshani/BusyIndicator  5
      0sys&clr-namespace:System;assembly=mscorlib  5      ] YDevExpress.Mvvm.v23.2, Version=23.2.3.0, Culture=neutral, PublicKeyToken=b88d1754d700e49aAdxmvvm2http://schemas.devexpress.com/winfx/2008/xaml/mvvm   5      K
viewmodels8clr-namespace:NetErp.Books.AccountingAccounts.ViewModels   5      ,ioc clr-namespace:NetErp.IoContainer   5      -   5        $��ucAccountDetail��5      $��Verdana)�5      $��Light"�6#   $��14(�66     � 	Resources  5      +   %NetErp.Helpers.StringToStyleConverter   StringToStyleConverter1  +DevExpress.Mvvm.UI.BooleanNegationConverter  BooleanNegationConverter!   NetErp.Helpers.BindingProxy  DataContextProxy   Data  IndicatorScaleX  IndicatorScaleY��%�   &	        &	      &	 &     &	 B     &	 [       5   
   6#     5   
   6*     5   
    62   �� X�5   
   3Q�60   X�5   
   3Q�60   5      2  ,DevExpress.Mvvm.UI.Interactivity.Interaction   	Behaviors 5      '  !DevExpress.Mvvm.UI.EventToCommand  5   
   �  �DevExpress.Mvvm.UI.Interactivity.EventTriggerBase`1[[System.Windows.DependencyObject, WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]   	EventName$ Loaded��6    +  %DevExpress.Mvvm.UI.EventToCommandBase   Command 64   �� *Data.InitializeCommand+ �� Source# [ .��5        BusyIndicator.BusyMask  -   5          Name$ BusyIndicator��5         IndicatorType$ Pulse=�5         BusyContent$ Please wait...��5       	   BusyContentMargin$	 0,20,0,0q�5!      
   IsBusyAtStartup$
 False��5"      
������    IsBusy 5      �� *IsBusy+.��5%   
   �  � RowDefinitions 5&   
   �� 5'      $
��auto �6   �� 5(      .+�5*   
   `� $��0��6   $��LeftToRight=�6#    `� IsLocked$
 True��6?   $��3��6[    `� 
Background
 ���� $��
0 10 10 10q�6�   .�5+      c� -   5         c� Band$ 1��5+       c� 	BandIndex$ 0��6   
������ .@�5,      �� $��2q�6    c� OverflowMode$ AsNeeded=�6$   ��6D   �� *1/Data.ReturnToAccountingAccountPlanMasterCommand+# [ ��5-      �� 5.       �� Orientation$ 
Horizontal=�6%   .+�5/      �� $c��]pack://application:,,,/DevExpress.Images.v23.2;component/Images/Navigation/Backward_32x32.png��6$   $
��None=�6�   $��2q�6�   ��50   "   �� 51   &    �� 
TargetType" ��6,    �� Triggers 52   *   P� 53   .    P� Property � {� P� Value$ False��6K   . �54   2   ��  �� Property �@J�Opacity �� Value$	 0.5Q�6L   55   /   57   '   59      �� 5:      $��Regresar��6(   $��2q�68   $��Center=�6C   5;       � ToolTip 5=      _� 5>      $��Regresar a PUC��6"   5@      �� 5A      $��5 0 5 0q�6   �� 5B      $��2q�6   $ AsNeeded=�6$   ��6D   �� *Data.SaveCommand+# [ ��5C      �� 5D      $ 
Horizontal=�6%   .+�5E      �� $Y��Spack://application:,,,/DevExpress.Images.v23.2;component/Images/Save/Save_32x32.png��6$   $
��None=�6�   $��2q�6�   ��5F   "   �� 5G   &   " ��6,    5H   *   P� 5I   .    � {�$ False��6K   . �5J   2   ��  �@J�Opacity$	 0.5Q�6L   5K   /   5M   '   5O      �� 5P      $��Guardar��6(   $��2q�67   $��Center=�6B   5Q       5S      _� 5T      $��Guardar cambios��6"   5V      5W      5X      � 5Z   
   
������ &	    NetErp.Helpers.EnterKeyTraversal 	  	IsEnabled$
 True��6(   $��1��6M    � ColumnDefinitions 5[      �� 5\      $��1* �6#   �� 5]      $��8* �6#   �� 5^      $��1* �6#   .+�5a      � -   5        $��GridDetails��5a      $��1��6&   $��Center=�66    5b      �� 5d      $
��auto �6'   �� 5f      $	��140 �6'   �� 5h      $��* �6'    5j      �� 5k      $��40 �6$   �� 5l      $
��auto �6$   �� 5m      $
��auto �6$   �� 5n      $
��auto �6$   �� 5o      $
��auto �6$   �� 5p      $
��auto �6$   �� 5q      $
��auto �6$   �� 5r      $
��auto �6$   �� 5s      $
��auto �6$   �� 5t      $
��auto �6$   �� 5u      $
��auto �6$   .+�5x      �� $��0��6   $��0��6)   $!��DETALLES DE CUENTA CONTABLE��69   $��3��6\   $��Center=�6p    �� Padding$ 10q�6�   $��22(�6�   �� 5{      $��1��6   $��0��6)   $��Clase��69   $��Center=�6F   $��10 5 0 5q�6a   y�6s   ��  �� Path$ Data.Lv1Visibility� �� Mode$ TwoWay=� �� UpdateSourceTrigger$ PropertyChanged=�# [ �� 5|      
�������$��1��5      $��1��6#   $��	10 5 10 5q�5�      $��3q�6)     �� TextAlignment$  Center=�5�      $��0��5�      ! �� 
IsReadOnly$
! True��5�      ��5|      �� $ Data.Lv1Code�$ TwoWay=�$ PropertyChanged=�# [ y�5}      �� $ Data.Lv1Visibility�$ TwoWay=�$ PropertyChanged=�# [ 5�      �� 5�      
�������$��5��5�      $��1��5�      $��2��6#   " �� CharacterCasing$" Upper=�5�      $��0 5 10 5q�5�      $��3q�6(   ��5�      �� $ Data.Lv1Name�$ TwoWay=�$ PropertyChanged=�# [ y�5�      �� $ Data.Lv1Visibility�$ TwoWay=�$ PropertyChanged=�# [ ! 5�      �� $ Data.IsReadOnlyLv1Name�$ TwoWay=�$ PropertyChanged=�# [ .��5�   4    5�      &
   DevExpress.Mvvm.UI.FocusBehavior
  5�      # 
  PropertyName$# Lv1NameIsFocused��6|   �  �DevExpress.Mvvm.UI.Interactivity.EventTriggerBase`1[[System.Windows.Controls.Control, PresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]]$   SourceObject$ 6/   �� $
 Data�# [ 6|   .��5�   4   5�      �� 5�      $��2��6   $��0��6)   $��Grupo��69   $��Center=�6F   $��10 5 0 5q�6a   y�6s   �� $ Data.Lv2Visibility�$ TwoWay=�$ PropertyChanged=�# [ �� 5�      
�������$��2��5�      $��1��6#   $��	10 5 10 5q�5�      $��3q�6)   $��1��5�      $  Center=�5�      $
! True��5�      ��5�      �� $ Data.Lv2Code�$ TwoWay=�$ PropertyChanged=�# [ y�5�      �� $ Data.Lv2Visibility�$ TwoWay=�$ PropertyChanged=�# [ 5�      �� 5�      
�������$��6��5�      $��2��5�      $��2��6#   $" Upper=�5�      $��0 5 10 5q�5�      $��3q�6(   ��5�      �� $ Data.Lv2Name�$ TwoWay=�$ PropertyChanged=�# [ y�5�      �� $ Data.Lv2Visibility�$ TwoWay=�$ PropertyChanged=�# [ ! 5�      �� $ Data.IsReadOnlyLv2Name�$ TwoWay=�$ PropertyChanged=�# [ .��5�   4    5�      
  5�      $# Lv2NameIsFocused��6|   $ 6/   �� $
 Data�# [ 6|   .��5�   4   5�      �� 5�      $��3��6   $��0��6)   $��Cuenta��69   $��Center=�6G   $��10 5 0 5q�6b   y�6t   �� $ Data.Lv3Visibility�$ TwoWay=�$ PropertyChanged=�# [ �� 5�      $��3��5�      $��1��6#   $��	10 5 10 5q�5�      $��3q�6)   
�������$��2��5�      $  Center=�5�      $
! True��5�      ��5�      �� $ Data.Lv3Code�$ TwoWay=�$ PropertyChanged=�# [ y�5�      �� $ Data.Lv3Visibility�$ TwoWay=�$ PropertyChanged=�# [ 5�      �� 5�      $��3��5�      $��2��6#   $��7��5�      $��0 5 10 5q�5�      $��3q�6(   $" Upper=�5�      
���������5�      �� $ Data.Lv3Name�$ TwoWay=�$ PropertyChanged=�# [ y�5�      �� $ Data.Lv3Visibility�$ TwoWay=�$ PropertyChanged=�# [ ! 5�      �� $ Data.IsReadOnlyLv3Name�$ TwoWay=�$ PropertyChanged=�# [ .��5�   .    5�      
  5�      $# Lv3NameIsFocused��6|   $ 6/   �� $
 Data�# [ 6|   .��5�   4   5�      �� 5�      $��4��6   $��0��6)   $��
Sub Cuenta��69   $��Center=�6K   $��10 5 0 5q�6f   y�6x   �� $ Data.Lv4Visibility�$ TwoWay=�$ PropertyChanged=�# [ �� 5�      $��4��5�      $��1��6#   $��	10 5 10 5q�5�      $��3q�6)   
�������$��3��5�      $  Center=�5�      $
! True��5�      ��5�      �� $ Data.Lv4Code�$ TwoWay=�$ PropertyChanged=�# [ y�5�      �� $ Data.Lv4Visibility�$ TwoWay=�$ PropertyChanged=�# [ 5�      �� 5�      $��4��5�      $��2��6#   $��8��5�      $��0 5 10 5q�5�      $��3q�6(   $" Upper=�5�      
���������5�      �� $ Data.Lv4Name�$ TwoWay=�$ PropertyChanged=�# [ y�5�      �� $ Data.Lv4Visibility�$ TwoWay=�$ PropertyChanged=�# [ ! 5�      �� $ Data.IsReadOnlyLv4Name�$ TwoWay=�$ PropertyChanged=�# [ .��5�   .    5�      
  5�      $# Lv4NameIsFocused��6|   $ 6/   �� $
 Data�# [ 6|   .��5�   4   5�      �� 5�      $��5��6   $��0��6)   $��Auxiliar��69   $��Center=�6I   $��10 5 0 5q�6d   y�6v   �� $ Lv5Visibility�$ TwoWay=�$ PropertyChanged=��� 5�      -   5        % ��Name$% Lv5Code��5�      $��5��5�      $��1��6"   $��4��5�      & �� 	MaxLength$& 8��5�      $��	10 5 10 5q�5�      $��3q�6(   
�������$  Center=�5�      ��5�      �� $ Data.Lv5Code�$ TwoWay=�$ PropertyChanged=�# [ y�5�      �� $ Data.Lv5Visibility�$ TwoWay=�$ PropertyChanged=�# [ ! 5�      �� $ Data.IsReadOnlyLv5Code�$ TwoWay=�$ PropertyChanged=�# [ .��5�   ,   ��5�      �� 5�      " ��6     5�      P� 5�   "    �@J�	IsFocused$
 True��6?   . �5�   &   ��  � ��
 �����6C   5�   #   .�5�      ��  � ��$ Stretch=�6B   �� 5�       � ��$ Stretch=�6D   5�      .��5�   %    5�      
  5�      $# Lv5CodeIsFocused��6|   $ 6/   �� $
 Data�# [ 6|   .��5�   4   5�      �� 5�      -   5        $��Lv5Name��5�      $��5��5�      $��2��6#   $��0 5 10 5q�5�      $��3q�6(   $��9��5      $" Upper=�5     
���������5�      �� $ Data.Lv5Name�$ TwoWay=�$ PropertyChanged=�# [ y�5�      �� $ Data.Lv5Visibility�$ TwoWay=�$ PropertyChanged=�# [ ��5     �� $ Lv5NameStyle�$ TwoWay=�$ PropertyChanged=�' �� 	Converter#' [  ! 5     �� $ Data.IsReadOnlyLv5Name�$ TwoWay=�$ PropertyChanged=�# [ .��5  .    5     
  5     $# Lv5NameIsFocused��6|   $ 6/   �� $
 Data�# [ 6|   .��5  4   5     �� 5     $��6��6   $��0��6)   $��
Naturaleza��69   $��Center=�6K   $��10 5 0 5q�6f   �� 5     ( �� DisplayMemberPath$( Value��5     ) �� SelectedValuePath$	) Key��5     $��6��5     $��1��6$   ${�False��5     $��False��5     $��	10 5 10 5q�5     $��3q�6*   $��3��5     ��5     �� $ Data.AccountNature�$ OneWay=�$ PropertyChanged=�# [ * �� SelectedValue* 5     �� *Data.SelectedAccountNature+$ TwoWay=�$ PropertyChanged=�# [ 5     5     5     5     5     5     