using Grocery.Core.Data.Helpers;
using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Microsoft.Data.Sqlite;

namespace Grocery.Core.Data.Repositories
{
    // ============================================
    // UC18: BOODSCHAPPENLIJSTITEMS IN DATABASE
    // ============================================
    // Deze repository haalt nu boodschappenlijstitems uit de database
    // We gebruiken UC17 (GroceryListRepository) als voorbeeld
    // We erven van DatabaseConnection om toegang te krijgen tot de database
    public class GroceryListItemsRepository : DatabaseConnection, IGroceryListItemsRepository
    {
        // Deze lijst gebruiken we tijdelijk om items in op te slaan
        private readonly List<GroceryListItem> groceryListItems = [];

        // ============================================
        // CONSTRUCTOR - wordt aangeroepen bij opstarten
        // ============================================
        public GroceryListItemsRepository()
        {
            // STAP 1: Maak de tabel aan in de database als deze nog niet bestaat
            // Dit gebeurt automatisch bij het starten van de app
            // De tabel heeft 4 kolommen: Id, GroceryListId, ProductId, Amount
            CreateTable(@"CREATE TABLE IF NOT EXISTS GroceryListItem (
                            [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            [GroceryListId] INTEGER NOT NULL,
                            [ProductId] INTEGER NOT NULL,
                            [Amount] INTEGER NOT NULL)");

            // STAP 2: Voeg wat standaard data toe aan de database
            // INSERT OR IGNORE zorgt ervoor dat we geen duplicaten krijgen
            // Als het item al bestaat (op basis van Id), wordt het genegeerd
            List<string> insertQueries = [
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(1, 1, 1, 3)",
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(2, 1, 2, 1)",
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(3, 1, 3, 4)",
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(4, 2, 1, 2)",
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(5, 2, 2, 5)"
            ];
            
            // Voer alle insert queries uit in één keer (dit is sneller en veiliger)
            // Dit is een transaction - als één query faalt, worden ze allemaal teruggedraaid
            InsertMultipleWithTransaction(insertQueries);
            
            // Haal alle items op uit de database zodat ze beschikbaar zijn
            GetAll();
        }

        // ============================================
        // GETALL - Haal ALLE boodschappenlijstitems op uit de database
        // ============================================
        public List<GroceryListItem> GetAll()
        {
            // Maak de lijst eerst leeg, anders krijgen we duplicaten
            groceryListItems.Clear();
            
            // Dit is de SQL query om alle items op te halen
            // SELECT betekent: geef me gegevens terug
            // FROM GroceryListItem betekent: uit de GroceryListItem tabel
            string selectQuery = "SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem";
            
            // Open de verbinding met de database
            // Dit moet altijd gebeuren voordat we een query kunnen uitvoeren
            OpenConnection();
            
            // Gebruik een SqliteCommand om de query uit te voeren
            // 'using' zorgt ervoor dat het command automatisch wordt opgeruimd na gebruik
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                // ExecuteReader geeft ons de resultaten terug als een SqliteDataReader
                // Hiermee kunnen we door de resultaten heen loopen
                SqliteDataReader reader = command.ExecuteReader();

                // Loop door alle resultaten
                // Read() geeft true terug zolang er nog rijen zijn, en false als we klaar zijn
                while (reader.Read())
                {
                    // Haal de gegevens op uit elke rij
                    // GetInt32(0) betekent: haal het eerste veld op (Id) als een integer
                    // GetInt32(1) betekent: haal het tweede veld op (GroceryListId)
                    // etc.
                    int id = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    
                    // Maak een nieuw GroceryListItem object met deze gegevens
                    // en voeg het toe aan onze lijst
                    groceryListItems.Add(new(id, groceryListId, productId, amount));
                }
            }
            
            // Sluit de database verbinding weer
            // Dit is belangrijk om de database niet 'vast' te houden
            CloseConnection();
            
            // Geef de lijst met alle items terug
            return groceryListItems;
        }

        // ============================================
        // GETALLONGROCERYLISTID - Haal items op voor een specifieke boodschappenlijst
        // ============================================
        public List<GroceryListItem> GetAllOnGroceryListId(int id)
        {
            // Deze query haalt alleen items op waar GroceryListId gelijk is aan de meegegeven id
            // WHERE is een filter: het betekent "alleen rijen die aan deze voorwaarde voldoen"
            string selectQuery = $"SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE GroceryListId = {id}";
            
            // Maak een nieuwe tijdelijke lijst voor de resultaten
            List<GroceryListItem> items = [];
            
            // Open de database verbinding
            OpenConnection();
            
            // Voer de query uit, net zoals bij GetAll()
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();

                // Loop door alle gevonden items
                while (reader.Read())
                {
                    int itemId = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    
                    // Voeg elk item toe aan de lijst
                    items.Add(new(itemId, groceryListId, productId, amount));
                }
            }
            
            // Sluit de verbinding
            CloseConnection();
            
            // Geef alleen de items terug die bij deze boodschappenlijst horen
            return items;
        }

        // ============================================
        // ADD - Voeg een NIEUW item toe aan de database
        // ============================================
        public GroceryListItem Add(GroceryListItem item)
        {
            // INSERT query om een nieuw item toe te voegen
            // We gebruiken @GroceryListId, @ProductId, etc. als placeholders
            // RETURNING RowId geeft ons het nieuwe Id terug dat de database heeft aangemaakt
            string insertQuery = $"INSERT INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(@GroceryListId, @ProductId, @Amount) RETURNING RowId;";
            
            // Open de database verbinding
            OpenConnection();
            
            using (SqliteCommand command = new(insertQuery, Connection))
            {
                // Gebruik parameters om SQL injection te voorkomen
                // Dit is veiliger dan de waarden direct in de query te zetten
                // @GroceryListId wordt vervangen door de echte waarde van item.GroceryListId
                command.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("ProductId", item.ProductId);
                command.Parameters.AddWithValue("Amount", item.Amount);

                // ExecuteScalar geeft ons één waarde terug: het nieuwe Id
                // Convert.ToInt32 zet deze waarde om naar een integer
                // We slaan dit op in item.Id zodat het item nu zijn database Id heeft
                item.Id = Convert.ToInt32(command.ExecuteScalar());
            }
            
            // Sluit de verbinding
            CloseConnection();
            
            // Geef het item terug, nu met zijn nieuwe Id
            return item;
        }

        // ============================================
        // DELETE - Verwijder een item uit de database
        // ============================================
        public GroceryListItem? Delete(GroceryListItem item)
        {
            // DELETE query om een item te verwijderen op basis van Id
            // WHERE Id = {item.Id} zorgt ervoor dat alleen dit specifieke item wordt verwijderd
            string deleteQuery = $"DELETE FROM GroceryListItem WHERE Id = {item.Id};";
            
            // Open de database verbinding
            OpenConnection();
            
            // ExecuteNonQuery voert een query uit die geen resultaten teruggeeft
            // (zoals DELETE, UPDATE, of INSERT zonder RETURNING)
            Connection.ExecuteNonQuery(deleteQuery);
            
            // Sluit de verbinding
            CloseConnection();
            
            // Geef het verwijderde item terug als bevestiging
            return item;
        }

        // ============================================
        // GET - Haal EEN specifiek item op uit de database
        // ============================================
        public GroceryListItem? Get(int id)
        {
            // Query om één item op te halen op basis van Id
            // WHERE Id = {id} zorgt ervoor dat we alleen het item met dit specifieke Id krijgen
            string selectQuery = $"SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE Id = {id}";
            
            // Deze variabele blijft null als we niks vinden
            // Het vraagteken (?) betekent dat de variabele null kan zijn
            GroceryListItem? groceryListItem = null;
            
            // Open de database verbinding
            OpenConnection();
            
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();

                // Als we een resultaat vinden (Read() geeft true terug)
                // We gebruiken 'if' in plaats van 'while' omdat we maar één item verwachten
                if (reader.Read())
                {
                    int itemId = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    
                    // Maak een nieuw GroceryListItem met de gevonden gegevens
                    groceryListItem = new(itemId, groceryListId, productId, amount);
                }
            }
            
            // Sluit de verbinding
            CloseConnection();
            
            // Geef het item terug (of null als we niks hebben gevonden)
            return groceryListItem;
        }

        // ============================================
        // UPDATE - Wijzig een bestaand item in de database
        // ============================================
        public GroceryListItem? Update(GroceryListItem item)
        {
            // UPDATE query om een item te wijzigen
            // SET betekent: verander deze velden naar deze nieuwe waarden
            // WHERE Id = {item.Id} zorgt ervoor dat alleen dit specifieke item wordt gewijzigd
            string updateQuery = $"UPDATE GroceryListItem SET GroceryListId = @GroceryListId, ProductId = @ProductId, Amount = @Amount WHERE Id = {item.Id};";
            
            // Open de database verbinding
            OpenConnection();
            
            using (SqliteCommand command = new(updateQuery, Connection))
            {
                // Gebruik parameters voor de nieuwe waarden
                // Dit is veiliger dan de waarden direct in de query te zetten
                command.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("ProductId", item.ProductId);
                command.Parameters.AddWithValue("Amount", item.Amount);

                // ExecuteNonQuery voert de query uit en geeft het aantal aangepaste rijen terug
                // We gebruiken deze waarde niet, maar de query wordt wel uitgevoerd
                command.ExecuteNonQuery();
            }
            
            // Sluit de verbinding
            CloseConnection();
            
            // Geef het gewijzigde item terug
            return item;
        }
    }
}