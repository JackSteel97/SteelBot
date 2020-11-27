using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SteelBot.Database.Models
{
    public class Poll
    {
        public long RowId { get; set; }

        [MaxLength(255)]
        public string Title { get; set; }

        public DateTime CreatedAt { get; set; }
        public ulong MessageId { get; set; }
        public ulong ChannelId { get; set; }
        public long UserRowId { get; set; }
        public bool IsLockedPoll { get; set; }

        public User PollCreator { get; set; }

        public List<PollOption> Options { get; set; }

        /// <summary>
        /// Empty constructor.
        /// Used by EF do not remove.
        /// </summary>
        public Poll() { }

        public Poll(string title, long pollCreatorId, ulong messageId, List<string> options, ulong channelId, bool lockedPoll = false)
        {
            Title = title;
            UserRowId = pollCreatorId;
            MessageId = messageId;
            Options = new List<PollOption>();
            ChannelId = channelId;
            IsLockedPoll = lockedPoll;

            for (int i = 0; i < options.Count; i++)
            {
                Options.Add(new PollOption(options[i], i + 1));
            }
            CreatedAt = DateTime.UtcNow;
        }

        public Poll Clone()
        {
            Poll pollCopy = (Poll)this.MemberwiseClone();
            pollCopy.Options = Options.ConvertAll(opt => opt.Clone());
            return pollCopy;
        }
    }
}