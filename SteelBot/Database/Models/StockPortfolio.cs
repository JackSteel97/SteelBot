using AlphaVantage.Net.Stocks;
using ScottPlot;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        public List<StockPortfolioSnapshot> Snapshots { get; set; }

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

        public async Task<StockPortfolioSnapshot> GenerateSnapshot(StockPriceService stockPriceService)
        {
            decimal totalValue = 0;
            foreach (OwnedStock stock in OwnedStock)
            {
                GlobalQuote quote = await stockPriceService.GetStock(stock.Symbol);
                if (quote != null)
                {
                    totalValue += quote.Price * stock.AmountOwned;
                }
            }

            return new StockPortfolioSnapshot(this, totalValue);
        }

        public decimal GetLastSnapshotValue()
        {
            decimal lastSnapshotValue = 0;
            if (Snapshots.Count > 0)
            {
                lastSnapshotValue = Snapshots[^1].TotalValueDollars;
            }
            return lastSnapshotValue;
        }

        public Plot GenerateValueHistoryChart()
        {
            double[] xValues = new double[Snapshots.Count];
            double[] yValues = new double[Snapshots.Count];

            for (int i = 0; i < Snapshots.Count; i++)
            {
                xValues[i] = Snapshots[i].SnapshotTaken.ToOADate();
                yValues[i] = Convert.ToDouble(Snapshots[i].TotalValueDollars);
            }

            Plot plt = new Plot();
            plt.Style(Style.Gray1);
            //plt.AddScatter(xValues, yValues);
            plt.AddFill(xValues, yValues);
            plt.SetAxisLimits(xMin: xValues[0], xMax: xValues[^1], yMin: 0);
            plt.XAxis.DateTimeFormat(true);
            plt.XAxis.TickLabelStyle(Color.White);
            plt.YAxis.TickLabelStyle(Color.White);

            plt.Title("History of Portfolio Value");
            plt.YLabel("Value in USD ($)");

            return plt;
        }

        public Plot GeneratePortfolioBreakdownChart(Dictionary<string, GlobalQuote> quotesBySymbol)
        {
            double[] values = new double[OwnedStock.Count];
            string[] labels = new string[OwnedStock.Count];
            int index = 0;

            foreach (OwnedStock stock in OwnedStock)
            {
                double value = 0;
                if (quotesBySymbol.TryGetValue(stock.Symbol, out GlobalQuote quote))
                {
                    value = Convert.ToDouble(quote.Price * stock.AmountOwned);
                }
                values[index] = value;
                labels[index] = $"{stock.Symbol}";

                index++;
            }

            var plt = new Plot();
            plt.Style(Style.Gray1);
            plt.Title("Portfolio Breakdown");

            ScottPlot.Plottable.PiePlot pie = plt.AddPie(values);
            pie.GroupNames = labels;
            pie.ShowPercentages = true;
            pie.ShowLabels = true;
            pie.Explode = true;
            pie.DonutSize = 0.6;
            plt.Legend();

            return plt;
        }
    }
}