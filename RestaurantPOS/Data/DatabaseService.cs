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
        
        public async Task<MenuCategory[]> GetCategoriesOfMenuItem(int menuItemId)
        {
            var query = @"
                            SELECT cat.*
                            FROM MenuCategory cat
                            INNER JOIN MenuItemCategoryMapping map ON cat.Id = map.MenuCategoryId
                            WHERE map.MenuItemId = ?
                        ";

            var categories = await _connection.QueryAsync<MenuCategory>(query, menuItemId);

            return [.. categories];
        }

        public async Task<string?> SaveMenuItemAsync(MenuItemModel model)
        {
            if (model.Id == 0)
            {
                // Creating a new menu item
                return await InsertMenuItemAsync(model);
            }
            else
            {
                // Updating an existing menu item
                return await UpdateMenuItemAsync(model);
            }
        }

        private async Task<string?> UpdateMenuItemAsync(MenuItemModel model)
        {
            string? errorMessage = null;

            await _connection.RunInTransactionAsync(db =>
            {
                var menuItem = db.Find<MenuItem>(model.Id);

                menuItem.Name = model.Name;
                menuItem.Description = model.Description;
                menuItem.Icon = model.Icon;
                menuItem.Price = model.Price;

                if (db.Update(menuItem) == 0)
                {
                    // this operation failed
                    // return error message
                    errorMessage = "Error in updating menu item";
                    throw new Exception(); // to trigger rollback
                }

                var deleQuery = @"
                                        DELETE FROM MenuItemCategoryMapping
                                        WHERE MenuItemId = ?
                                    ";

                db.Execute(deleQuery, menuItem.Id);

                var categoryMapping = model.MenuCategories.Where(c => c.IsSelected)
                                                        .Select(c => new MenuItemCategoryMapping
                                                        {
                                                            MenuCategoryId = c.Id,
                                                            MenuItemId = menuItem.Id,
                                                        });

                if (db.InsertAll(categoryMapping) == 0)
                {
                    errorMessage = "Error in updating menu item";
                    throw new Exception(); // to trigger rollback
                }
            });

            return errorMessage;
        }

        private async Task<string?> InsertMenuItemAsync(MenuItemModel model)
        {
            var menuItem = new MenuItem
            {
                Name = model.Name,
                Description = model.Description,
                Icon = model.Icon,
                Price = model.Price,
            };

            if (await _connection.InsertAsync(menuItem) > 0
                && await InsertMenuItemCategoryMappingAsync(model, menuItem))
            {
                return null;
            }

            return "Error in saving menu item";
        }

        private async Task<bool> InsertMenuItemCategoryMappingAsync(MenuItemModel model, MenuItem menuItem)
        {
            var categoryMapping = model.MenuCategories.Where(c => c.IsSelected)
                                                                        .Select(c => new MenuItemCategoryMapping
                                                                        {
                                                                            MenuCategoryId = c.Id,
                                                                            MenuItemId = menuItem.Id,
                                                                        });

            if (await _connection.InsertAllAsync(categoryMapping) > 0)
            {
                model.Id = menuItem.Id;
                return true;
            }
            else
            {
                // Menu item insert was successful
                // but Category mapping insert operation failed
                // we should remove the newly inserted menu item from the database
                await _connection.DeleteAsync(menuItem);
                return false;               
            }
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