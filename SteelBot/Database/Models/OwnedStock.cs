using System;

namespace SteelBot.Database.Models
{
    public class OwnedStock
    {
        public long RowId { get; set; }
        public StockPortfolio ParentPortfolio { get; set; }
        public long ParentPortfolioRowId { get; set; }
        public string Symbol { get; set; }
        public decimal AmountOwned { get; set; }
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Empty constructor.
        /// Used by EF - do not remove.
        /// </summary>
        public OwnedStock() { }

        public OwnedStock(StockPortfolio parentPortfolio, string symbol, decimal starterAmount, DateTime creationTime)
        {
            ParentPortfolioRowId = parentPortfolio.RowId;
            Symbol = symbol.ToUpper();
            AmountOwned = starterAmount;
            LastUpdated = creationTime;
        }

        public OwnedStock Clone()
        {
            return (OwnedStock)MemberwiseClone();
        }
    }
}