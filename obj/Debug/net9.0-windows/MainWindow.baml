   M S B A M L   `   `   `  ����C  ?GunVault, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null#clr-namespace:GunVaultGunVault  5   	       GunVault.MainWindow   5   	   W SPresentationCore, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   (localclr-namespace:GunVault     5   	   -   5          � Title$  GunVault - Рогалик��5   	   $	��720��6$   $
��1280��61    � WindowStartupLocation$ CenterScreen=�5	   	    � WindowState$ 	Maximized=�5
   	    � 
ResizeMode$ 	CanResize=�5   	   .��5      �  � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   .+�5   
   �� $��0��6    �� Orientation$ 
Horizontal=�6"   ���#333333$��50��6P    K� ZIndex$
 1000��6\   .+�5      �� -   5         ��Name$ 
HealthText��5      $��Здоровье: 100��6,   
�������$��18(�6T   $��20,0,0,0q�6b   $��Center=�6t   �� 5      -   5        $ 
WeaponText��5      $$��Оружие: Пистолет��6,   
�������$��18(�6W   $��40,0,0,0q�6e   $��Center=�6w   �� 5      -   5        $ AmmoText��5      $��Патроны: 12/12��6*   
�������$��18(�6S   $��40,0,0,0q�6a   $��Center=�6s   �� 5      -   5        $ 	ScoreText��5      $��Счёт: 0��6+   
�������$��18(�6M   $��40,0,0,0q�6[   $��Center=�6m   �� 5      -   5        $ DebugInfoText��5      $��Отладка��6/   ���#AAFFAA$��16(�6S   $��40,0,0,0q�6a   $��Center=�6s   $y�	Collapsed=�6�   5      �� 5   
   -   5         ��Name$ 
GameCanvas��5      $��1��6%   ���#222222$
|�True��6G   $ 0��6X   � 5"   
   -   5        	 �Name$	 LoadingScreen��5"      $��1��6&   ���#222222$y�	Collapsed=�6H   $
 2000��6_   .+�5#      �� $��Center=�6   .+�5$      �� $��ЗАГРУЗКА МИРА��6   ���#FFD700$��32(�6F   $
��Bold"�6T   $��Center=�5%      $��0,0,0,30q�69   � 5&      -	   5        
 �Name$
 LoadingProgressBar��5&      $	��500��6:   $��20��6F    � Maximum$	 100Q�6R    � Value$ 0Q�6`   ���#4CAF50���#333333���#5555555'   H   �� 5(      -
   5        $ LoadingStatusText��5(      $#��Инициализация...��67   
�������$��18(�5)      $��Center=�6*   $��0,15,0,0q�6G   �� 5*      -   5        $ LoadingProgressText��5*      $��0%��69   
�������$��18(�5+      $��Center=�6*   $��0,10,0,0q�6G   5,      5-      �� 50   
   -   5         ��Name$ WeaponNotification��50      $��1��6-   $y�	Collapsed=�6:   ���	#80000000$��Center=�51      $	��Top=�6.    �� CornerRadius$ 	0,0,10,10��6F    �� Padding$ 20,15q�52      $	��500��6!   ���#FFD700$��0,0,2,2q�6F   $
 1000��53      $��0,0,0,0q�6%   ._�54      �� .+�55      �� $��НОВОЕ ОРУЖИЕ!��6   ���#FFD700$��20(�6F   $
��Bold"�6T   $��Center=�56      $��0,0,0,10q�69   �� 57      -   5        $ NotificationWeaponName��57      $#��Название оружия��6<   
�������$��18(�58      $��Center=�6*   59      5:      5;      5<      