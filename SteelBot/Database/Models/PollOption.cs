using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text;

namespace SteelBot.Database.Models
{
    public class PollOption
    {
        public long RowId { get; set; }
        public long PollRowId { get; set; }

        [MaxLength(255)]
        public string OptionText { get; set; }
        public int OptionNumber { get; set; }

        public Poll Poll { get; set; }

        /// <summary>
        /// Empty constructor.
        /// Used by EF do not remove.
        /// </summary>
        public PollOption() { }

        public PollOption(string option, int optionNumber)
        {
            OptionText = option;
            OptionNumber = optionNumber;
        }

        public PollOption Clone()
        {
            PollOption optionCopy = (PollOption)this.MemberwiseClone();
            return optionCopy;
        }
    }
}
