using Nexar.Client;
using System;

namespace Nexar.Comment.Types
{
    sealed class WorkspaceTag : TagType<IMyWorkspace>
    {
        public WorkspaceTag(IMyWorkspace tag) : base(tag)
        { }

        public IDisposable CommentSubscription { get; set; }

        public override string ToString()
        {
            return Tag.Name;
        }
    }
}
