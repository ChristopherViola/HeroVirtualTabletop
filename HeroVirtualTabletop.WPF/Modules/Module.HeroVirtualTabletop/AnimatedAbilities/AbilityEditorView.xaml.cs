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
using Framework.WPF.Extensions;
using Module.HeroVirtualTabletop.Library.Utility;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    /// <summary>
    /// Interaction logic for AbilityEditorView.xaml
    /// </summary>
    public partial class AbilityEditorView : UserControl
    {
        private AbilityEditorViewModel viewModel;
        private IAnimationElement selectedAnimationElementRoot;
        public AbilityEditorView(AbilityEditorViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
            this.viewModel.EditModeEnter += viewModel_EditModeEnter;
            this.viewModel.EditModeLeave += viewModel_EditModeLeave;
            this.viewModel.AnimationAdded += viewModel_AnimationAddedUpdateTreeView;
            this.viewModel.AnimationAdded += ViewModel_AnimationAddedUpdateDataGrid;
        }

        private void ViewModel_AnimationAddedUpdateDataGrid(object sender, EventArgs e)
        {
            AnimationElement animationAdded = sender as AnimationElement;
            switch (animationAdded.Type)
            {
                case Library.Enumerations.AnimationType.FX:
                    dataGridAnimationResource.SetBinding(DataGrid.ItemsSourceProperty, new Binding("FXElements"));
                    break;
            }
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

        private void viewModel_AnimationAddedUpdateTreeView(object sender, EventArgs e)
        {
            IAnimationElement modelToSelect = sender as IAnimationElement;
            if (sender == null) // need to unselect
            {
                DependencyObject dObject = treeViewAnimations.GetItemFromSelectedObject(treeViewAnimations.SelectedItem);
                TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                if (tvi != null)
                {
                    tvi.IsSelected = false;
                    this.selectedAnimationElementRoot = null;
                }
            }
            else
            {
                bool itemFound = false;
                TextBox txtBox = null;
                treeViewAnimations.UpdateLayout();
                if (sender is IAnimationElement)
                {
                    if (this.viewModel.SelectedAnimationElement == null || !(this.viewModel.SelectedAnimationElement is SequenceElement)) // A new animation has been added to the collection
                    {
                        for (int i = 0; i < treeViewAnimations.Items.Count; i++)
                        {
                            TreeViewItem item = treeViewAnimations.ItemContainerGenerator.ContainerFromItem(treeViewAnimations.Items[i]) as TreeViewItem;
                            if (item != null)
                            {
                                var model = item.DataContext as IAnimationElement;
                                if (model.Name == modelToSelect.Name)
                                {
                                    item.IsSelected = true;
                                    itemFound = true;
                                    txtBox = FindTextBoxInTemplate(item);
                                    this.viewModel.SelectedAnimationElement = model as IAnimationElement;
                                    this.viewModel.SelectedAnimationParent = null;
                                    this.selectedAnimationElementRoot = null;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!itemFound && this.viewModel.SelectedAnimationElement is SequenceElement) // Added somewhere in nested animation
                {
                    DependencyObject dObject = null;
                    if (this.selectedAnimationElementRoot != null && this.viewModel.SelectedAnimationElement != null)
                    {
                        TreeViewItem item = treeViewAnimations.ItemContainerGenerator.ContainerFromItem(this.selectedAnimationElementRoot) as TreeViewItem;
                        dObject = FindTreeViewItemUnderTreeViewItemByModelName(item, this.viewModel.SelectedAnimationElement.Name);
                    }
                    else
                        dObject = treeViewAnimations.GetItemFromSelectedObject(this.viewModel.SelectedAnimationElement);
                    TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                    if (tvi != null)
                    {
                        IAnimationElement model = tvi.DataContext as IAnimationElement;
                        if (tvi.Items != null)
                        {
                            tvi.IsExpanded = true;
                            tvi.UpdateLayout();
                            for (int i = 0; i < tvi.Items.Count; i++)
                            {
                                TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                                if (item != null)
                                {
                                    model = item.DataContext as IAnimationElement;
                                    if (model.Name == modelToSelect.Name)
                                    {
                                        item.IsSelected = true;
                                        itemFound = true;
                                        item.UpdateLayout();
                                        txtBox = FindTextBoxInTemplate(item);
                                        this.viewModel.SelectedAnimationElement = model as IAnimationElement;
                                        this.viewModel.SelectedAnimationParent = tvi.DataContext as IAnimationElement;
                                        if (this.selectedAnimationElementRoot == null)
                                            this.selectedAnimationElementRoot = tvi.DataContext as IAnimationElement;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                //if (txtBox != null)
                //    this.viewModel.EnterEditModeCommand.Execute(txtBox);
            }
        }

        private TextBox FindTextBoxInTemplate(TreeViewItem item)
        {
            TextBox textBox = Helper.GetDescendantByType(item, typeof(TextBox)) as TextBox;
            return textBox;
        }
        private TreeViewItem FindTreeViewItemUnderTreeViewItemByModelName(TreeViewItem tvi, string modelName)
        {
            TreeViewItem treeViewItemRet = null;
            if (tvi != null)
            {
                IAnimationElement model = tvi.DataContext as IAnimationElement;
                if (model.Name == modelName)
                {
                    return tvi;
                }
                else if (tvi.Items != null)
                {
                    for (int i = 0; i < tvi.Items.Count; i++)
                    {
                        TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                        var treeViewItem = FindTreeViewItemUnderTreeViewItemByModelName(item, modelName);
                        if (treeViewItem != null)
                        {
                            treeViewItemRet = treeViewItem;
                            break;
                        }
                    }
                }
            }
            return treeViewItemRet;
        }
        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                TreeViewItem item = GetRootTreeViewItemParent(treeViewItem);
                if (item != null)
                    this.selectedAnimationElementRoot = item.DataContext as IAnimationElement;
                else
                    this.selectedAnimationElementRoot = null;
                if (treeViewItem.DataContext is SequenceElement)
                {
                    treeViewItem = GetImmediateTreeViewItemParent(treeViewItem);
                    if (treeViewItem != null)
                        this.viewModel.SelectedAnimationParent = treeViewItem.DataContext as IAnimationElement;
                    else
                        this.viewModel.SelectedAnimationParent = null;

                }
                else
                    this.viewModel.SelectedAnimationParent = null;
            }
        }
        private TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }
        private TreeViewItem GetContainerFromItem(ItemsControl parent, object item)
        {
            var found = parent.ItemContainerGenerator.ContainerFromItem(item);
            if (found == null)
            {
                for (int i = 0; i < parent.Items.Count; i++)
                {
                    var childContainer = parent.ItemContainerGenerator.ContainerFromIndex(i) as ItemsControl;
                    TreeViewItem childFound = null;
                    if (childContainer != null)
                    {
                        bool expanded = (childContainer as TreeViewItem).IsExpanded;
                        (childContainer as TreeViewItem).IsExpanded = true;
                        childFound = GetContainerFromItem(childContainer, item);
                        (childContainer as TreeViewItem).IsExpanded = childFound == null ? expanded : true;
                    }
                    if (childFound != null)
                    {
                        (childContainer as TreeViewItem).IsExpanded = true;
                        return childFound;
                    }

                }
            }
            return found as TreeViewItem;
        }

        private TreeViewItem GetImmediateTreeViewItemParent(TreeViewItem treeViewItem)
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
            return treeViewItem;
        }

        private TreeViewItem GetRootTreeViewItemParent(TreeViewItem treeViewItem)
        {
            DependencyObject dObject = VisualTreeHelper.GetParent(treeViewItem); // got the immediate parent
            treeViewItem = dObject as TreeViewItem; // now get first treeview item parent
            while (true)
            {
                dObject = VisualTreeHelper.GetParent(dObject);
                if (dObject is TreeViewItem)
                    treeViewItem = dObject as TreeViewItem;
                else if (dObject is TreeView)
                    break;
            }
            return treeViewItem;
        }
    }
}
