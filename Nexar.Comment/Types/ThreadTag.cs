using Nexar.Client;

namespace Nexar.Comment.Types
{
    sealed class ThreadTag : TagType<IMyThread>
    {
        public ProjectTag Project { get; }

        public ThreadTag(IMyThread tag, ProjectTag project) : base(tag)
        {
            Project = project;
        }

        public override string ToString()
        {
            return Tag.Comments.Count == 0 ? "<none>" : Tag.Comments[0].Text;
        }
    }
}
