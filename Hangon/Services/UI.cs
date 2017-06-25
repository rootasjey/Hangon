using Microsoft.Toolkit.Uwp.UI.Animations;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Hangon.Services {
    public static class UI {
        public static DependencyObject FindChildControl<T>(DependencyObject control, string ctrlName) {
            int childNumber = VisualTreeHelper.GetChildrenCount(control);
            for (int i = 0; i < childNumber; i++) {
                DependencyObject child = VisualTreeHelper.GetChild(control, i);
                FrameworkElement fe = child as FrameworkElement;
                // Not a framework element or is null
                if (fe == null) return null;

                if (child is T && fe.Name == ctrlName) {
                    // Found the control so return
                    return child;
                } else {
                    // Not found it - search children
                    DependencyObject nextLevel = FindChildControl<T>(child, ctrlName);
                    if (nextLevel != null)
                        return nextLevel;
                }
            }
            return null;
        }

        public static T GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        public static T GetParentOfType<T>(this DependencyObject depObj) where T : DependencyObject {
            if (depObj == null) return null;

            var parent = VisualTreeHelper.GetParent(depObj);

            var result = (parent as T) ?? GetParentOfType<T>(parent);
            if (result != null) return result;
            return null;
        }

        public async static Task ScrollToIndex(this ListViewBase listViewBase, int index) {
            bool isVirtualizing = default(bool);
            double previousHorizontalOffset = default(double), previousVerticalOffset = default(double);

            // get the ScrollViewer withtin the ListView/GridView
            //var scrollViewer = listViewBase.GetScrollViewer();
            var scrollViewer = listViewBase.GetChildOfType<ScrollViewer>();

            // get the SelectorItem to scroll to
            var selectorItem = listViewBase.ContainerFromIndex(index) as SelectorItem;

            // when it's null, means virtualization is on and the item hasn't been realized yet
            if (selectorItem == null) {
                isVirtualizing = true;

                previousHorizontalOffset = scrollViewer.HorizontalOffset;
                previousVerticalOffset = scrollViewer.VerticalOffset;

                // call task-based ScrollIntoViewAsync to realize the item
                await listViewBase.ScrollIntoViewAsync(listViewBase.Items[index]);

                // this time the item shouldn't be null again
                selectorItem = (SelectorItem)listViewBase.ContainerFromIndex(index);
            }

            // calculate the position object in order to know how much to scroll to
            var transform = selectorItem.TransformToVisual((UIElement)scrollViewer.Content);
            var position = transform.TransformPoint(new Point(0, 0));

            // when virtualized, scroll back to previous position without animation
            if (isVirtualizing) {
                await scrollViewer.ChangeViewAsync(previousHorizontalOffset, previousVerticalOffset, true);
            }

            // scroll to desired position with animation!
            scrollViewer.ChangeView(position.X, position.Y, null);
        }

        public async static Task ScrollToItem(this ListViewBase listViewBase, object item) {
            bool isVirtualizing = default(bool);
            double previousHorizontalOffset = default(double), previousVerticalOffset = default(double);

            // get the ScrollViewer withtin the ListView/GridView
            //var scrollViewer = listViewBase.GetScrollViewer();
            var scrollViewer = listViewBase.GetChildOfType<ScrollViewer>();

            // get the SelectorItem to scroll to
            var selectorItem = listViewBase.ContainerFromItem(item) as SelectorItem;

            // when it's null, means virtualization is on and the item hasn't been realized yet
            if (selectorItem == null) {
                isVirtualizing = true;

                previousHorizontalOffset = scrollViewer.HorizontalOffset;
                previousVerticalOffset = scrollViewer.VerticalOffset;

                // call task-based ScrollIntoViewAsync to realize the item
                await listViewBase.ScrollIntoViewAsync(item);

                // this time the item shouldn't be null again
                selectorItem = (SelectorItem)listViewBase.ContainerFromItem(item);
            }

            // calculate the position object in order to know how much to scroll to
            var transform = selectorItem.TransformToVisual((UIElement)scrollViewer.Content);
            var position = transform.TransformPoint(new Point(0, 0));

            // when virtualized, scroll back to previous position without animation
            if (isVirtualizing) {
                await scrollViewer.ChangeViewAsync(previousHorizontalOffset, previousVerticalOffset, true);
            }

            // scroll to desired position with animation!
            scrollViewer.ChangeView(position.X, position.Y, null);
        }

        public static async Task ScrollIntoViewAsync(this ListViewBase listViewBase, object item) {
            var tcs = new TaskCompletionSource<object>();
            //var scrollViewer = listViewBase.GetScrollViewer();
            var scrollViewer = listViewBase.GetChildOfType<ScrollViewer>();

            EventHandler<ScrollViewerViewChangedEventArgs> viewChanged = (s, e) => tcs.TrySetResult(null);
            try {
                scrollViewer.ViewChanged += viewChanged;
                listViewBase.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
                await tcs.Task;
            } finally {
                scrollViewer.ViewChanged -= viewChanged;
            }
        }

        public static async Task ChangeViewAsync(this ScrollViewer scrollViewer, double? horizontalOffset, double? verticalOffset, bool disableAnimation) {
            var tcs = new TaskCompletionSource<object>();

            EventHandler<ScrollViewerViewChangedEventArgs> viewChanged = (s, e) => tcs.TrySetResult(null);
            try {
                scrollViewer.ViewChanged += viewChanged;
                scrollViewer.ChangeView(horizontalOffset, verticalOffset, null, disableAnimation);
                await tcs.Task;
            } finally {
                scrollViewer.ViewChanged -= viewChanged;
            }
        }
        
        public static void AnimateBackItemToList(ListViewBase list, object lastSelectedItem, ConnectedAnimation connectedAnimation) {
            list.ScrollIntoView(lastSelectedItem);

            if (list.GetType() == typeof(ListView)) {
                var item = (ListViewItem)list.ContainerFromItem(lastSelectedItem);
                if (item == null) { connectedAnimation.Cancel(); return; }

                var pane = (FrameworkElement)item.ContentTemplateRoot;

                var image = (Image)pane.FindName("PhotoImage");
                if (image == null) { connectedAnimation.Cancel(); return; }

                image.Opacity = 0;
                image.Loaded += (_s, _e) => {
                    image.Opacity = 1;
                    connectedAnimation.TryStart(image);
                };

            } else if (list.GetType() == typeof(AdaptiveGridView) || list.GetType() == typeof(GridView)) {
                var item = (GridViewItem)list.ContainerFromItem(lastSelectedItem);
                if (item == null) { connectedAnimation.Cancel(); return; }

                var pane = (FrameworkElement)item.ContentTemplateRoot;

                var image = (Image)pane.FindName("PhotoImage");
                if (image == null) { connectedAnimation.Cancel(); return; }

                image.Opacity = 0;
                image.Loaded += (_s, _e) => {
                    image.Opacity = 1;
                    connectedAnimation.TryStart(image);
                };
            }
        }

        public static async Task AnimateSlideIn(this Panel view) {
            view.Opacity = 0;
            view.Visibility = Visibility.Visible;

            List<double> opacities = new List<double>();

            var children = view.Children;
            foreach (var child in children) {
                opacities.Add(child.Opacity);
                child.Opacity = 0;
                await child.Offset(0, 20, 0).StartAsync();
            }

            view.Opacity = 1;

            AnimateView();

            void AnimateView()
            {
                int index = 0;
                var delay = 0;
                foreach (var child in children) {
                    delay += 200;
                    child.Fade((float)opacities[index], 1000, delay)
                         .Offset(0, 0, 1000, delay)
                         .Start();
                    index++;
                }
            }
        }

        //public static async Task AnimateDeepSlideIn(this Panel view) {
        //    view.Opacity = 0;
        //    view.Visibility = Visibility.Visible;

        //    List<double> opacities = new List<double>();

        //    var children = view.Children;            

        //    foreach (var child in children) {
        //        opacities.Add(child.Opacity);
        //        child.Opacity = 0;
        //        await child.Offset(0, 20, 0).StartAsync();

        //        Panel childPanel = child.GetChildOfType<StackPanel>();
        //        if (childPanel == null) child.GetChildOfType<Grid>();
        //        if (childPanel != null) {
        //            await AnimateDeepSlideIn(childPanel);
        //        }
        //    }

        //    view.Opacity = 1;

        //    AnimateView();

        //    void AnimateView()
        //    {
        //        int index = 0;
        //        var delay = 0;
        //        foreach (var child in children) {
        //            delay += 200;
        //            child.Fade((float)opacities[index], 500, delay)
        //                 .Offset(0, 0, 500, delay)
        //                 .Start();
        //            index++;
        //        }
        //    }
        //}
    }
}
