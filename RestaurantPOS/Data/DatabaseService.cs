using RestaurantPOS.Models;
using SQLite;

namespace RestaurantPOS.Data
{
    public class DatabaseService : IAsyncDisposable
    {
        private readonly SQLiteAsyncConnection _connection;

        public DatabaseService()
        {
            //C:\\Users\\PC\\AppData\\Local\\restaurant_pos.db3
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "restaurant_pos.db3");

            _connection = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
        }

        public async Task InitializeDatabaseAsync()
        {
            await _connection.CreateTableAsync<MenuCategory>();
            await _connection.CreateTableAsync<MenuItem>();
            await _connection.CreateTableAsync<MenuItemCategoryMapping>();
            await _connection.CreateTableAsync<Order>();
            await _connection.CreateTableAsync<OrderItem>();

            await SeedDataAsync();
        }

        private async Task SeedDataAsync()
        {
            var firstCategory = await _connection.Table<MenuCategory>().FirstOrDefaultAsync();
            if (firstCategory != null)
            {
                return; // database already seeded
            }

            var categories = SeedData.GetMenuCategories();
            var menuItems = SeedData.GetMenuItems();
            var mappings = SeedData.GetMenuItemCategoryMappings();

            await _connection.InsertAllAsync(categories);
            await _connection.InsertAllAsync(menuItems);
            await _connection.InsertAllAsync(mappings);
        }

        public async Task<MenuCategory[]> GetMenuCategoriesAsync() => await _connection.Table<MenuCategory>().ToArrayAsync();

        public async Task<MenuItem[]> GetMenuItemByCategoryAsync(int categoryId)
        {
            var query = @"
                        SELECT menu.*
                        FROM MenuItem as menu
                        INNER JOIN MenuItemCategoryMapping as mapping ON menu.Id == mapping.MenuItemId
                        Where mapping.MenuCategoryId = ?
                        ";

            var menuItems = await _connection.QueryAsync<MenuItem>(query, categoryId);

            return [.. menuItems];
        }

        public async Task<string?> PlaceOrderAsync(OrderModel model)
        {
            var order = new Order
            {
                OrderDate = model.OrderDate,
                PaymentMode = model.PaymentMode,
                TotalAmountPaid = model.TotalAmountPaid,
                TotalItemCount = model.TotalItemCount,
            };

            if (await _connection.InsertAsync(order) > 0)
            {
                // Order inserted successfully
                // now we have newly inserted order id in order.id
                // Now we can add the orderId to the OrderItems and Insert OrderItems to the database
                foreach (var item in model.Items)
                {
                    item.OrderId = order.Id;
                }

                if (await _connection.InsertAllAsync(model.Items) == 0)
                {
                    // OrderItems insert operation failed
                    // Remove the newly inserted order in this method
                    await _connection.DeleteAsync(order);

                    return "Error in inserting order items";
                }
                
                model.Id = order.Id;
                return null;
            }

            return "Error in inserting the order";
        }
        
        public async Task<Order[]> GetOrdersAsync()
        {
            return await _connection.Table<Order>().ToArrayAsync();
        }

        public async Task<OrderItem[]> GetOrderItemsAsync(long orderId)
        {
            return await _connection.Table<OrderItem>().Where(c => c.OrderId == orderId).ToArrayAsync();
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
            }
        }
    }
}