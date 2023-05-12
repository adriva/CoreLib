using System.Collections.Generic;
using Tweetinvi.Models;

namespace Adriva.Extensions.Social
{
    public class Hashtag
    {

        public string Text { get; }

        public Hashtag(string text)
        {
            this.Text = text;
        }
    }
}