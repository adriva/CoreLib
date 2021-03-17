using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adriva.Common.Core;
using Microsoft.Extensions.Options;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace Adriva.Extensions.Social.Twitter
{
    public class TwitterClient : ISocialMediaClient
    {
        protected TwitterOptions Options { get; private set; }

        protected TwitterCredentials Credentials { get; private set; }

        public TwitterClient(IOptions<TwitterOptions> optionsAccessor)
        {
            this.Options = optionsAccessor.Value;
            this.Credentials = new TwitterCredentials(this.Options.Key, this.Options.Secret, this.Options.AccessToken, this.Options.AccessTokenSecret);
        }

        public async Task<Post> PublishAsync(Post post)
        {
            var tweet = await Auth.ExecuteOperationWithCredentials(this.Credentials, () =>
            {
                var parameters = new PublishTweetOptionalParameters();

                if (null != post.Media)
                {
                    var binaryMediaItems = post.Media.OfType<BinaryMedia>();

                    foreach (var binaryMediaItem in binaryMediaItems)
                    {
                        parameters.MediaBinaries.Add(binaryMediaItem.Data.ToArray());
                    }
                }

                return TweetAsync.PublishTweet(post.Text, parameters);
            });

            if (null == tweet)
            {
                throw new SocialMediaException($"Failed to publish tweet.");
            }
            string postText = post.Text;

            if (0 < post.HashTags.Count)
            {
                StringBuilder buffer = new StringBuilder();
                foreach (var hashtag in post.HashTags)
                {
                    buffer.Append(" ");
                    if (!hashtag.Text.StartsWith("#", StringComparison.Ordinal))
                    {
                        buffer.Append("#");
                    }
                    buffer.Append(hashtag.Text);
                }
                postText += buffer.ToString();
            }

            Post output = new Post(tweet.IdStr, postText);
            return output;
        }

        public async Task<IEnumerable<Post>> GetPostsAsync(string userId, DynamicItem configuration)
        {
            if (string.IsNullOrWhiteSpace(userId)) return Array.Empty<Post>();

            var tweets = await Auth.ExecuteOperationWithCredentials(this.Credentials, () =>
            {
                var parameters = new UserTimelineParameters()
                {
                    MaximumNumberOfTweetsToRetrieve = 20,
                    ExcludeReplies = true,
                    IncludeContributorDetails = false,
                    IncludeRTS = false,
                    IncludeEntities = true,
                    TrimUser = true
                };

                var userIdentifier = new UserIdentifier(userId);
                return Tweetinvi.TimelineAsync.GetUserTimeline(userIdentifier, parameters);
            });

            return tweets.Select(t =>
            {
                var post = new Post(t.IdStr, t.FullText);
                foreach (var hashtag in t.Hashtags)
                {
                    post.HashTags.Add(new Hashtag(hashtag.Text));
                }
                return post;
            });
        }
    }
}