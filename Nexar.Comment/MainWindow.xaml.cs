using Nexar.Client;
using Nexar.Comment.Properties;
using Nexar.Comment.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shell;

namespace Nexar.Comment
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The current thread with comments in the list view.
        /// </summary>
        ThreadTag _ThreadTag;

        /// <summary>
        /// Old window placement.
        /// </summary>
        WindowPlacement _WindowPlacement;

        public MainWindow()
        {
            InitializeComponent();

            // show the endpoint in the title
            Title = $"Login... {Config.ApiEndpoint}";

            // load as a task after the window is shown
            Task.Run(async () =>
            {
                // login
                await App.LoginAsync();

                // load data
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // begin
                    Title = $"Loading... {Config.ApiEndpoint}";
                    TaskbarItemInfo = new TaskbarItemInfo { ProgressState = TaskbarItemProgressState.Indeterminate };

                    // activate, after browser windows this window may be passive
                    Activate();
                    Topmost = true;
                    Topmost = false;
                    Focus();

                    Task.Run(async () =>
                    {
                        // get data
                        await App.LoadWorkspacesAsync();

                        // show data
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // populate the tree with to be expanded workspaces
                            foreach (var workspace in App.Workspaces)
                                MyTree.Items.Add(Tree.CreateItem(new WorkspaceTag(workspace), true));

                            // end
                            Title = $"Nexar.Comment Demo - {Config.ApiEndpoint}";
                            TaskbarItemInfo = new TaskbarItemInfo { ProgressState = TaskbarItemProgressState.None };
                        });
                    });
                });
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // load window placement
            try
            {
                _WindowPlacement = Settings.Default.WindowPlacement;
                WindowPlacement.Set(this, _WindowPlacement);
            }
            catch
            { }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // save window placement
            var newPlacement = WindowPlacement.Get(this);
            if (!newPlacement.Equals(_WindowPlacement))
            {
                Settings.Default.WindowPlacement = newPlacement;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Fetches workspace projects and populates the specified tree item.
        /// </summary>
        private void PopulateWorkspaceItem(TreeViewItem item)
        {
            var workspace = (WorkspaceTag)item.Tag;
            item.Items.Clear();

            // fetch projects
            var projects = Task.Run(async () =>
            {
                var client = workspace.GetNexarClient();
                var res = await client.Projects.ExecuteAsync(workspace.Tag.Url);
                res.AssertNoErrors();
                return res.Data.DesProjects.Nodes.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
            }).Result;

            // populate
            foreach (var project in projects)
                item.Items.Add(Tree.CreateItem(new ProjectTag(project, workspace), true));
        }

        /// <summary>
        /// Fetches project threads and populates the specified tree item.
        /// </summary>
        private void PopulateProjectItem(TreeViewItem item)
        {
            var project = (ProjectTag)item.Tag;
            var workspace = project.Workspace;
            item.Items.Clear();

            // fetch threads
            var threads = Task.Run(async () =>
            {
                var client = workspace.GetNexarClient();
                var res = await client.CommentThreads.ExecuteAsync(project.Tag.Id);
                res.AssertNoErrors();
                return res.Data.DesCommentThreads;
            }).Result;

            // populate
            foreach (var thread in threads.OrderByDescending(x => x.ThreadNumber))
                item.Items.Add(Tree.CreateItem(new ThreadTag(thread, project), false));

            // subscribe to comment updates once per workspace
            if (workspace.CommentSubscription == null && Config.IsSubscription)
            {
                // fetch OnCommentUpdated
                var client = workspace.GetNexarClient();
                var watch = client.OnCommentUpdated.Watch(new DesOnCommentUpdatedInput
                {
                    WorkspaceUrl = workspace.Tag.Url,
                    Token = Config.AccessToken
                });

                // subscribe
                workspace.CommentSubscription = watch.Subscribe(res => Application.Current.Dispatcher.Invoke(() =>
                {
                    res.AssertNoErrors();
                    using (new WaitCursor())
                    {
                        var dto = res.Data.DesOnCommentUpdated;
                        OnCommentUpdated(dto.Action, dto.Data.ProjectId, dto.Data.CommentThreadId);
                    }
                }));
            }
        }

        /// <summary>
        /// Called by comment subscription and F5.
        /// </summary>
        private void OnCommentUpdated(string action, string projectId, string threadGuid)
        {
            if (action == "CommentChanged" || action == "CommentThreadChanged")
            {
                // get old thread tree item and tag, skip missing
                var threadItem1 = Tree.FindItem(
                    MyTree.Items,
                    x => x.Tag is ThreadTag tag && tag.Tag.CommentThreadId == threadGuid
                );
                if (threadItem1 == null)
                    return;
                var thread1 = (ThreadTag)threadItem1.Tag;

                // fetch new thread
                var client = thread1.Project.Workspace.GetNexarClient();
                var thread2 = Task.Run(() => client.CommentThread.ExecuteAsync(
                    thread1.Project.Tag.Id,
                    threadGuid)
                ).Result.Data.DesCommentThread;

                // replace comments with updated
                var oldText = thread1.ToString();
                thread1.ReplaceTag(thread2);

                // update comments
                if (_ThreadTag == thread1)
                    PopulateCommentList(_ThreadTag);

                // update tree item if thread text changed
                if (oldText != thread1.ToString())
                {
                    var items = ((TreeViewItem)threadItem1.Parent).Items;
                    var index = items.IndexOf(threadItem1);
                    if (index >= 0)
                    {
                        //! cache selected state
                        bool isSelected = threadItem1.IsSelected;

                        // reset item
                        var threadItem2 = Tree.CreateItem(thread1, false);
                        items[index] = threadItem2;

                        // restore selected
                        if (isSelected)
                            threadItem2.IsSelected = true;
                    }
                    items.Refresh();
                }
                return;
            }

            if (action == "CommentThreadAdded" || action == "CommentThreadDeleted")
            {
                // get old project tree item and tag, skip missing
                var projectItem = Tree.FindItem(
                    MyTree.Items,
                    x => x.Tag is ProjectTag tag && tag.Tag.Id == projectId
                );
                if (projectItem == null)
                    return;

                // clear deleted list
                bool deleted = action == "CommentThreadDeleted" && _ThreadTag.Tag.CommentThreadId == threadGuid;
                if (deleted)
                {
                    _ThreadTag = null;
                    MyList.Items.Clear();
                }

                // find current thread
                string selectedThreadGuid = null;
                foreach (var it in projectItem.Items)
                {
                    if (it is TreeViewItem item && item.IsSelected)
                    {
                        selectedThreadGuid = ((ThreadTag)item.Tag).Tag.CommentThreadId;
                        break;
                    }
                }

                // populate
                PopulateProjectItem(projectItem);

                // update selected item and thread
                foreach (TreeViewItem item in projectItem.Items)
                {
                    var newThreadTag = (ThreadTag)item.Tag;

                    // set selected if it was selected
                    if (newThreadTag.Tag.CommentThreadId == selectedThreadGuid)
                        item.IsSelected = true;

                    // replace thread with new instance
                    if (_ThreadTag != null && _ThreadTag.Tag.CommentThreadId == newThreadTag.Tag.CommentThreadId)
                        _ThreadTag = newThreadTag;
                }
            }
        }

        public void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            // skip already expaned
            var item = e.Source as TreeViewItem;
            if (item.Items.Count == 0 || item.Items[0] != null)
                return;

            // populate expaned item
            using (new WaitCursor())
            {
                if (item.Tag is WorkspaceTag)
                {
                    PopulateWorkspaceItem(item);
                    return;
                }
                if (item.Tag is ProjectTag)
                {
                    PopulateProjectItem(item);
                    return;
                }
            }
        }

        /// <summary>
        /// Populates the comment list and sets the current thread.
        /// </summary>
        private void PopulateCommentList(ThreadTag thread)
        {
            // set the current
            _ThreadTag = thread;

            // populate
            int n = 0;
            MyList.Items.Clear();
            foreach (var comment in thread.Tag.Comments)
            {
                ++n;
                MyList.Items.Add(new
                {
                    Name = comment.CreatedBy.Email,
                    comment.CommentId,
                    comment.Text,
                    Visibility = n == 1 && !string.IsNullOrEmpty(thread.Tag.OriginalStateScreenshotUrl) ? Visibility.Visible : Visibility.Collapsed
                });
            }

            // scroll down
            if (MyList.Items.Count > 0)
            {
                var li = MyList.Items[MyList.Items.Count - 1];
                MyList.ScrollIntoView(li);
            }
        }

        // Avoid three view auto scroll to the right on long items.
        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        // If a thread is selected, populate its comment list
        private void MyTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag is ThreadTag thread)
            {
                using (new WaitCursor())
                    PopulateCommentList(thread);
            }
        }

        // `Ctrl+Enter` in the editor sends the comment.
        private void MyEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var item = (TreeViewItem)MyTree.SelectedItem;
                if (item.Tag is ThreadTag thread)
                {
                    var text = MyEdit.Text;
                    if (text.Trim().Length == 0)
                        return;

                    using (new WaitCursor())
                    {
                        // send mutation
                        var client = thread.Project.Workspace.GetNexarClient();
                        Task.Run(() => client.CreateComment.ExecuteAsync(new DesCreateCommentInput
                        {
                            EntityId = thread.Project.Tag.Id,
                            CommentThreadId = thread.Tag.CommentThreadId,
                            Text = text
                        })).Wait();

                        // refresh if subscription is off
                        if (!Config.IsSubscription)
                            OnCommentUpdated("CommentChanged", null, thread.Tag.CommentThreadId);
                    }

                    MyEdit.Text = string.Empty;
                }
            }
        }

        // Opens snapshot windows on link clicks.
        private void Hyperlink_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var url = _ThreadTag.Tag.OriginalStateScreenshotUrl;
            if (!string.IsNullOrEmpty(url))
            {
                var form = new ImageWindow(url);
                form.Title += $" - {_ThreadTag}";
                form.Show();
                form.Activate();
                e.Handled = true;
            }
        }

        // `F2` - open the selected workspace or project.
        // `F5` - force fetch the current thread.
        // `Delete` - remove the selected comment.
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // simulate "CommentChanged"
            if (e.Key == Key.F5 && _ThreadTag != null)
            {
                // fake notification
                using (new WaitCursor())
                    OnCommentUpdated("CommentChanged", null, _ThreadTag.Tag.CommentThreadId);
                return;
            }

            //! F1 is bad, started browser may see it pressed and open help
            // open selected workspace/project
            if (e.Key == Key.F2)
            {
                var item = (TreeViewItem)MyTree.SelectedItem;
                if (item == null)
                    return;

                if (item.Tag is WorkspaceTag workspaceTag)
                {
                    Process.Start(workspaceTag.Tag.Url);
                    return;
                }

                void ShowProject(ProjectTag tag)
                {
                    Process.Start(tag.Tag.Url);
                }

                if (item.Tag is ProjectTag projectTag)
                {
                    ShowProject(projectTag);
                }
                else if (item.Tag is ThreadTag threadTag)
                {
                    ShowProject(threadTag.Project);
                }
                return;
            }

            if (e.Key == Key.Delete)
            {
                // skip if no selected item
                var comment = (dynamic)MyList.SelectedItem;
                if (comment == null)
                    return;

                // confirm
                if (MessageBox.Show(
                    $"From: {comment.Name}:\r\n{comment.Text}",
                    "Delete comment?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question)
                    != MessageBoxResult.Yes)
                    return;

                // delete
                using (new WaitCursor())
                {
                    var client = _ThreadTag.Project.Workspace.GetNexarClient();
                    Task.Run(() => client.DeleteComment.ExecuteAsync(new DesDeleteCommentInput
                    {
                        EntityId = _ThreadTag.Project.Tag.Id,
                        CommentThreadId = _ThreadTag.Tag.CommentThreadId,
                        CommentId = comment.CommentId
                    })).Wait();

                    // refresh if subscription is off
                    if (!Config.IsSubscription)
                        OnCommentUpdated("CommentChanged", null, _ThreadTag.Tag.CommentThreadId);
                }
                return;
            }
        }
    }
}
