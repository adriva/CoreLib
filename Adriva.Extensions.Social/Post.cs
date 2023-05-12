using System.Collections.Generic;
using Tweetinvi.Models;

namespace Adriva.Extensions.Social
{
    public class Post
    {
        public string Id { get; }

        public string Text { get; }

        public IList<IMedia> Media { get; private set; } = new List<IMedia>();
        public IList<Hashtag> HashTags { get; private set; } = new List<Hashtag>();

        public Post(string text) : this(null, text)
        {

        }

        public Post(string id, string text)
        {
            this.Id = id;
            this.Text = text;
        }
    }
}