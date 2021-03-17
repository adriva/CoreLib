using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Adriva.Common.Core;
using Newtonsoft.Json;

namespace Adriva.Extensions.Notifications
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class NotificationMessage
    {
        [JsonProperty("recipients")]
        private readonly IList<Recipient> RecipientsList = new List<Recipient>();

        [JsonIgnore]
        public ReadOnlyCollection<Recipient> Recipients => new ReadOnlyCollection<Recipient>(this.RecipientsList);

        [JsonProperty("id")]
        public string Id { get; internal set; }

        [JsonProperty("priority")]
        public Priority Priority { get; private set; }

        [JsonProperty("target")]
        public NotificationTarget Target { get; private set; }

        [JsonProperty("data", DefaultValueHandling = DefaultValueHandling.Ignore, TypeNameHandling = TypeNameHandling.All)]
        public object Data { get; private set; }

        [JsonConstructor]
        private NotificationMessage() { }

        public static NotificationMessage Create(object data, IEnumerable<Recipient> recipients, NotificationTarget target)
        {
            NotificationMessage notificationMessage = new NotificationMessage()
            {
                Data = data,
                Target = target,
                Priority = Priority.Default
            };
            return notificationMessage.WithRecipients(recipients);
        }

        public NotificationMessage WithRecipients(IEnumerable<Recipient> recipients, bool replaceExisting = false)
        {
            if (null == recipients || !recipients.Any())
            {
                throw new ArgumentException($"A message should contain at least one recipient", nameof(recipients));
            }

            NotificationMessage message = this;

            if (replaceExisting) message.RecipientsList.Clear();

            foreach (var recipient in recipients)
            {
                message.RecipientsList.Add(recipient);
            }
            return message;
        }

        public NotificationMessage WithData(object data)
        {
            NotificationMessage message = this;
            message.Data = data;
            return message;
        }

        public NotificationMessage WithPriority(Priority priority)
        {
            NotificationMessage message = this;
            message.Priority = priority;
            return message;
        }

        public NotificationMessage WithTarget(NotificationTarget target)
        {
            NotificationMessage message = this;
            message.Target = target;
            return message;
        }

        public NotificationMessage WithId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (!string.IsNullOrWhiteSpace(this.Id)) throw new InvalidOperationException("Notification message already has an id.");
            NotificationMessage message = this;
            message.Id = id;
            return message;
        }
    }
}
