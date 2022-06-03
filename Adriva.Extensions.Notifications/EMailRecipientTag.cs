using System;
using Newtonsoft.Json;

namespace Adriva.Extensions.Notifications
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class EMailRecipientTag
    {
        public static readonly EMailRecipientTag To = new EMailRecipientTag("to");
        public static readonly EMailRecipientTag Cc = new EMailRecipientTag("cc");
        public static readonly EMailRecipientTag Bcc = new EMailRecipientTag("bcc");

        [JsonProperty("type")]
        public string Type { get; private set; }

        public static bool operator ==(EMailRecipientTag first, EMailRecipientTag second)
        {
            if (null == (object)first) return null == (object)second;
            if (null == (object)second) return false;

            return 0 == string.Compare(first.Type, second.Type, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(EMailRecipientTag first, EMailRecipientTag second)
        {
            if (null == (object)first) return null != (object)second;
            if (null == (object)second) return false;

            return 0 != string.Compare(first.Type, second.Type, StringComparison.OrdinalIgnoreCase);
        }

        [JsonConstructor]
        protected EMailRecipientTag(string type)
        {
            this.Type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj is EMailRecipientTag eMailRecipientTag)
            {
                return 0 == string.Compare(eMailRecipientTag.Type, this.Type, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return 2049151605 + StringComparer.OrdinalIgnoreCase.GetHashCode(this.Type);
        }
    }
}
