using System;

namespace SteelBot.Database.Models
{
    public class StockPortfolioSnapshot
    {
        public long RowId { get; set; }

        public long ParentPortfolioRowId { get; set; }
        public StockPortfolio ParentPortfolio { get; set; }
        public DateTime SnapshotTaken { get; set; }
        public decimal TotalValueDollars { get; set; }

        /// <summary>
        /// Empty constructor.
        /// Used by EF - do not remove.
        /// </summary>
        public StockPortfolioSnapshot() { }

        public StockPortfolioSnapshot(StockPortfolio parentPortfolio, decimal value)
        {
            ParentPortfolioRowId = parentPortfolio.RowId;
            SnapshotTaken = DateTime.UtcNow;
            TotalValueDollars = value;
        }
    }
}