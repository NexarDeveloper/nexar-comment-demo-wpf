using Nexar.Client;

namespace Nexar.Comment.Types
{
    sealed class ProjectTag : TagType<IMyProject>
    {
        public WorkspaceTag Workspace { get; }

        public ProjectTag(IMyProject tag, WorkspaceTag workspace) : base(tag)
        {
            Workspace = workspace;
        }

        public override string ToString()
        {
            return Tag.Name;
        }
    }
}
