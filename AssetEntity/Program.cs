using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AssetTracking
{
    class Program
    {
        static void Main(string[] args)
        {
            using var dbContext = new AssetContext();
            dbContext.Database.Migrate(); // Ensure the database is created and migrations are applied.

            Console.WriteLine("Välkommen till Asset Tracking System\n");

            while (true)
            {
                // Filter Assets by Country and Type
                var (selectedCountry, selectedCurrency) = GetCountryAndCurrency();
                if (selectedCountry == "Ogiltig") continue;

                var assetTypeFilter = GetAssetType();
                if (assetTypeFilter == null) continue;

                // Query filtered assets from the database
                var assets = dbContext.Assets
                    .Where(a => a.Country == selectedCountry && a.AssetType == assetTypeFilter)
                    .OrderByDescending(a => a.PurchaseDate)
                    .ToList();

                var conversionRates = GetCurrencyConversionRates(selectedCurrency);
                DisplayAssets(assets, conversionRates, selectedCurrency);

                if (!AskToSearchAgain()) break;
            }
        }

        private static (string, string) GetCountryAndCurrency()
        {
            Console.WriteLine("Välj Land/Kontor:\n1. USA (USD)\n2. Tyskland (EUR)\n3. Storbritannien (GBP)\n4. Sverige (SEK)");
            int choice = int.Parse(Console.ReadLine() ?? "0");
            return choice switch
            {
                1 => ("USA", "USD"),
                2 => ("Tyskland", "EUR"),
                3 => ("Storbritannien", "GBP"),
                4 => ("Sverige", "SEK"),
                _ => ("Ogiltig", "USD")
            };
        }

        private static string GetAssetType()
        {
            Console.WriteLine("Välj Tillgångstyp:\n1. Laptops/Datorer\n2. Mobiltelefoner");
            int choice = int.Parse(Console.ReadLine() ?? "0");
            return choice switch
            {
                1 => "Laptop",
                2 => "Mobiltelefon",
                _ => null
            };
        }

        private static Dictionary<string, decimal> GetCurrencyConversionRates(string baseCurrency)
        {
            var rates = new Dictionary<string, decimal> { { "USD", 1m }, { "EUR", 0.85m }, { "GBP", 0.75m }, { "SEK", 8.5m } };
            return rates.ToDictionary(rate => rate.Key, rate => rate.Value / rates[baseCurrency]);
        }

        private static void DisplayAssets(List<Asset> assets, Dictionary<string, decimal> conversionRates, string currency)
        {
            Console.WriteLine($"\nTillgång       Märke      Modell      Pris i {currency}      Datum");
            Console.WriteLine("------------------------------------------------------");
            foreach (var asset in assets)
            {
                decimal convertedPrice = asset.Price * (conversionRates.ContainsKey(currency) ? conversionRates[currency] : 1);
                Console.WriteLine($"{asset.AssetType,-12} {asset.Brand,-10} {asset.Model,-10} {convertedPrice.ToString("C", CultureInfo.CurrentCulture),-15} {asset.PurchaseDate.ToShortDateString()}");
            }
        }

        private static bool AskToSearchAgain()
        {
            Console.WriteLine("\nVill du söka igen? (Y/N)");
            return char.ToUpper(Console.ReadKey().KeyChar) == 'Y';
        }
    }

    public class Asset
    {
        public int Id { get; set; }
        public string AssetType { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public decimal Price { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Country { get; set; }
    }

    public class AssetContext : DbContext
    {
        public DbSet<Asset> Assets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=localhost\SQLEXPRESS;Database=AssetTrackingDB;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }
}
