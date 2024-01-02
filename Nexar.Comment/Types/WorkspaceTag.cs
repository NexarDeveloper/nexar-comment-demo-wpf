using Nexar.Client;
using System;

namespace Nexar.Comment.Types
{
    sealed class WorkspaceTag : TagType<IMyWorkspace>
    {
        private NexarClient _nexarClient;

        public WorkspaceTag(IMyWorkspace tag) : base(tag)
        {
        }

        public IDisposable CommentSubscription { get; set; }

        public override string ToString()
        {
            return Tag.Name;
        }

        public NexarClient GetNexarClient()
        {
            if (_nexarClient == null)
            {
                // subscription is currently not supported and works only with internal infrastructure services
                var apiServiceUrl = Config.IsSubscription ? Config.ApiEndpoint : Tag.Location.ApiServiceUrl;
                _nexarClient = NexarClientFactory.CreateClient(apiServiceUrl, Config.AccessToken);
            }

            return _nexarClient;
        }
    }
}
