using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Database.Models
{
    public class StockPortfolio
    {
        public long RowId { get; set; }
        public long OwnerRowId { get; set; }
        public User Owner { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<OwnedStock> OwnedStock { get; set; }

        public Dictionary<string, OwnedStock> OwnedStockBySymbol { get; set; }

        /// <summary>
        /// Empty constructor.
        /// Used by EF - do not remove.
        /// </summary>
        public StockPortfolio() { }

        public StockPortfolio(User owner)
        {
            Created = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            Owner = owner;
            OwnerRowId = owner.RowId;
        }

        public void BuildOwnedStockCache()
        {
            OwnedStockBySymbol = OwnedStock.ToDictionary(os => os.Symbol);
        }
    }
}