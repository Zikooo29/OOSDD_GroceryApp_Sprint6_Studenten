using Grocery.Core.Data.Helpers;
using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Microsoft.Data.Sqlite;

namespace Grocery.Core.Data.Repositories
{
    // ═══════════════════════════════════════════════════════════
    // UC19 AANGEPAST: Nu erven we van DatabaseConnection
    // Voorheen: public class ProductRepository : IProductRepository
    // ═══════════════════════════════════════════════════════════
    public class ProductRepository : DatabaseConnection, IProductRepository
    {
        private readonly List<Product> products = [];

        // ═══════════════════════════════════════════════════════════
        // UC19 AANGEPAST: Constructor maakt nu database tabel aan
        // Voorheen: Hardcoded lijst met producten
        // ═══════════════════════════════════════════════════════════
        public ProductRepository()
        {
            // UC19 NIEUW: Maak Product tabel aan in database
            CreateTable(@"CREATE TABLE IF NOT EXISTS Product (
                            [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            [Name] NVARCHAR(80) NOT NULL,
                            [Stock] INTEGER NOT NULL,
                            [ShelfLife] DATE NOT NULL,
                            [Price] DECIMAL(5,2) NOT NULL)");

            // UC19 NIEUW: Voeg standaard producten toe aan database
            List<string> insertQueries = [
                @"INSERT OR IGNORE INTO Product(Id, Name, Stock, ShelfLife, Price) VALUES(1, 'Melk', 300, '2025-09-25', 0.95)",
                @"INSERT OR IGNORE INTO Product(Id, Name, Stock, ShelfLife, Price) VALUES(2, 'Kaas', 100, '2025-09-30', 7.98)",
                @"INSERT OR IGNORE INTO Product(Id, Name, Stock, ShelfLife, Price) VALUES(3, 'Brood', 400, '2025-09-12', 2.19)",
                @"INSERT OR IGNORE INTO Product(Id, Name, Stock, ShelfLife, Price) VALUES(4, 'Cornflakes', 0, '2025-12-31', 1.48)"
            ];

            InsertMultipleWithTransaction(insertQueries);
            GetAll();
        }

        // ═══════════════════════════════════════════════════════════
        // UC19 AANGEPAST: GetAll haalt producten op uit database
        // Voorheen: return products; (hardcoded lijst)
        // ═══════════════════════════════════════════════════════════
        public List<Product> GetAll()
        {
            products.Clear();

            string selectQuery = "SELECT Id, Name, Stock, date(ShelfLife), Price FROM Product";

            OpenConnection();

            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    int stock = reader.GetInt32(2);
                    DateOnly shelfLife = DateOnly.FromDateTime(reader.GetDateTime(3));
                    decimal price = reader.GetDecimal(4);

                    products.Add(new(id, name, stock, shelfLife, price));
                }
            }

            CloseConnection();
            return products;
        }

        // ═══════════════════════════════════════════════════════════
        // UC19 AANGEPAST: Get haalt 1 product op uit database
        // Voorheen: return products.FirstOrDefault(p => p.Id == id);
        // ═══════════════════════════════════════════════════════════
        public Product? Get(int id)
        {
            string selectQuery = $"SELECT Id, Name, Stock, date(ShelfLife), Price FROM Product WHERE Id = {id}";

            Product? product = null;

            OpenConnection();

            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    int productId = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    int stock = reader.GetInt32(2);
                    DateOnly shelfLife = DateOnly.FromDateTime(reader.GetDateTime(3));
                    decimal price = reader.GetDecimal(4);

                    product = new(productId, name, stock, shelfLife, price);
                }
            }

            CloseConnection();
            return product;
        }

        // ═══════════════════════════════════════════════════════════
        // UC19 BELANGRIJK: Add() is NU geïmplementeerd!
        // Voorheen: throw new NotImplementedException();
        // Dit is de kern van UC19 - nieuwe producten kunnen toevoegen
        // ═══════════════════════════════════════════════════════════
        public Product Add(Product item)
        {
            // UC19 NIEUW: INSERT query om product toe te voegen aan database
            string insertQuery = @"INSERT INTO Product(Name, Stock, ShelfLife, Price) 
                                   VALUES(@Name, @Stock, @ShelfLife, @Price) 
                                   RETURNING RowId;";

            OpenConnection();

            using (SqliteCommand command = new(insertQuery, Connection))
            {
                // UC19 NIEUW: Gebruik parameters voor veiligheid (voorkomt SQL injection)
                command.Parameters.AddWithValue("Name", item.Name);
                command.Parameters.AddWithValue("Stock", item.Stock);
                command.Parameters.AddWithValue("ShelfLife", item.ShelfLife.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("Price", item.Price);

                // UC19 NIEUW: Database geeft het nieuwe Id terug
                item.Id = Convert.ToInt32(command.ExecuteScalar());
            }

            CloseConnection();
            return item;
        }

        // ═══════════════════════════════════════════════════════════
        // UC19 AANGEPAST: Update wijzigt product in database
        // Voorheen: Vond product in lijst en wijzigde het
        // ═══════════════════════════════════════════════════════════
        public Product? Update(Product item)
        {
            string updateQuery = @"UPDATE Product 
                                   SET Name = @Name, Stock = @Stock, ShelfLife = @ShelfLife, Price = @Price 
                                   WHERE Id = @Id;";

            OpenConnection();

            using (SqliteCommand command = new(updateQuery, Connection))
            {
                command.Parameters.AddWithValue("Id", item.Id);
                command.Parameters.AddWithValue("Name", item.Name);
                command.Parameters.AddWithValue("Stock", item.Stock);
                command.Parameters.AddWithValue("ShelfLife", item.ShelfLife.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("Price", item.Price);

                command.ExecuteNonQuery();
            }

            CloseConnection();
            return item;
        }

        // ═══════════════════════════════════════════════════════════
        // UC19 AANGEPAST: Delete verwijdert product uit database
        // Voorheen: throw new NotImplementedException();
        // ═══════════════════════════════════════════════════════════
        public Product? Delete(Product item)
        {
            string deleteQuery = $"DELETE FROM Product WHERE Id = {item.Id};";

            OpenConnection();
            Connection.ExecuteNonQuery(deleteQuery);
            CloseConnection();

            return item;
        }
    }
}