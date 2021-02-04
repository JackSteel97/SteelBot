using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Database.Models
{
    public class OwnedStock
    {
        public long RowId { get; set; }
        public StockPortfolio ParentPortfolio { get; set; }
        public long ParentPortfolioRowId { get; set; }
        public string Symbol { get; set; }
        public double AmountOwned { get; set; }
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Empty constructor.
        /// Used by EF - do not remove.
        /// </summary>
        public OwnedStock() { }

        public OwnedStock(StockPortfolio parentPortfolio, string symbol, double starterAmount, DateTime creationTime)
        {
            ParentPortfolio = parentPortfolio;
            ParentPortfolioRowId = parentPortfolio.RowId;
            Symbol = symbol;
            AmountOwned = starterAmount;
            LastUpdated = creationTime;
        }
    }
}