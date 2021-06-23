using System;
using System.Windows.Controls;

namespace Nexar.Comment
{
    /// <summary>
    /// Tree view helper functions.
    /// </summary>
    static class Tree
    {
        /// <summary>
        /// Creates a tree item with the specified attached tag and expanding state.
        /// </summary>
        public static TreeViewItem CreateItem(object tag, bool toExpand)
        {
            var item = new TreeViewItem
            {
                Header = tag.ToString(),
                Tag = tag
            };

            if (toExpand)
                item.Items.Add(null);

            return item;
        }

        /// <summary>
        /// Recursive search for the specified tree item in the specified branch by the predicate.
        /// </summary>
        public static TreeViewItem FindItem(ItemCollection source, Func<TreeViewItem, bool> predicate)
        {
            foreach (TreeViewItem item in source)
            {
                if (item == null)
                    continue;

                if (predicate(item))
                    return item;

                var item2 = FindItem(item.Items, predicate);
                if (item2 != null)
                    return item2;
            }
            return null;
        }
    }
}
