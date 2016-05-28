﻿using Module.HeroVirtualTabletop.Roster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Module.HeroVirtualTabletop.Roster
{
    /// <summary>
    /// Interaction logic for RosterExplorerView.xaml
    /// </summary>
    public partial class RosterExplorerView : UserControl
    {
        private RosterExplorerViewModel viewModel;

        private bool isSingleClick = false;
        private bool isDoubleClick = false;
        private bool isTripleClick = false;
        private bool isQuadrupleClick = false;
        private int milliseconds = 0;
        private int maxClickTime = System.Windows.Forms.SystemInformation.DoubleClickTime * 4;
        private System.Windows.Forms.Timer clickTimer = new System.Windows.Forms.Timer();
        public RosterExplorerView(RosterExplorerViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.DataContext = this.viewModel;

            clickTimer.Interval = 50;
            clickTimer.Tick +=
                new EventHandler(clickTimer_Tick);
        }
        private void TextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.LeftButton == MouseButtonState.Pressed)
            {
                var ItemsPres = ((sender as TextBlock).Parent as Expander).Content as ItemsPresenter;
                var VStackPanel = VisualTreeHelper.GetChild(ItemsPres as DependencyObject, 0) as VirtualizingStackPanel;
                foreach (ListBoxItem item in VStackPanel.Children)
                {
                    item.IsSelected = true;
                }
                e.Handled = true;
            }
        }
        private void ListViewItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                isSingleClick = true;

                // Start the click timer.
                clickTimer.Start();
            }
            // This is the second mouse click.
            else if (e.ClickCount == 2)
            {
                isDoubleClick = true;
            }
            else if (e.ClickCount == 3)
            {
                isTripleClick = true;
            }
            else if (e.ClickCount == 4)
            {
                isQuadrupleClick = true;
            }
            
        }

        void clickTimer_Tick(object sender, EventArgs e)
        {
            milliseconds += 50;

            if (milliseconds >= maxClickTime)
            {
                clickTimer.Stop();

                if (isQuadrupleClick)
                {
                    this.viewModel.ToggleManueverWithCamera();
                }
                else if(isTripleClick)
                {
                    this.viewModel.TargetAndFollow();
                }
                else if (isDoubleClick)
                {
                    this.viewModel.TargetAndFollow();
                }
                else
                {
                    this.viewModel.TargetOrFollow();
                }

                isSingleClick = isDoubleClick = isTripleClick = isQuadrupleClick = false;
                milliseconds = 0;
            }
        }
    }
}
