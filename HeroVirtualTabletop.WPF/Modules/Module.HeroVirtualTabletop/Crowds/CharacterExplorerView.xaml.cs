﻿using System;
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

namespace Module.HeroVirtualTabletop.Crowds
{
    /// <summary>
    /// Interaction logic for CharacterExplorerView.xaml
    /// </summary>
    public partial class CharacterExplorerView : UserControl
    {
        private CharacterExplorerViewModel viewModel;
        public CharacterExplorerView(CharacterExplorerViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
            this.viewModel.EditModeEnter+=viewModel_EditModeEnter;
            this.viewModel.EditModeLeave += viewModel_EditModeLeave;
        }

        private void viewModel_EditModeEnter(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            Grid grid = txtBox.Parent as Grid;
            TextBox textBox = grid.Children[1] as TextBox;
            textBox.Visibility = Visibility.Visible;
            textBox.Focus();
            textBox.SelectAll();
        }

        private void viewModel_EditModeLeave(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            txtBox.Visibility = Visibility.Hidden;
            BindingExpression expression = txtBox.GetBindingExpression(TextBox.TextProperty);
            expression.UpdateSource(); 
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                if (treeViewItem.DataContext is CrowdModel)
                {
                    DependencyObject dObject = VisualTreeHelper.GetParent(treeViewItem); // got the immediate parent
                    treeViewItem = dObject as TreeViewItem; // now get first treeview item parent
                    while (treeViewItem == null)
                    {
                        dObject = VisualTreeHelper.GetParent(dObject);
                        treeViewItem = dObject as TreeViewItem;
                        if (dObject is TreeView)
                            break;
                    }
                    if(treeViewItem != null)
                        this.viewModel.SelectedCrowdParent = treeViewItem.DataContext as CrowdModel;
                    else
                        this.viewModel.SelectedCrowdParent = null;
                }
                else
                    this.viewModel.SelectedCrowdParent = null;
            }
        }
        private TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }
    }
}
