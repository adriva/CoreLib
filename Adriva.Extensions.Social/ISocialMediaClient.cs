using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Microsoft.Extensions.Options;

namespace Adriva.Extensions.Social
{
    public interface ISocialMediaClient
    {
        Task<Post> PublishAsync(Post post);

        Task<IEnumerable<Post>> GetPostsAsync(string userId, DynamicItem configuration = null);
    }
}