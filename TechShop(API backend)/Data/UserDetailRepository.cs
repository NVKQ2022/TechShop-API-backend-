using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TechShop_API_backend_.Interfaces;
using TechShop_API_backend_.Models;
namespace TechShop_API_backend_.Data
{

    public class UserDetailRepository 
    {
        private readonly IMongoCollection<UserDetail> _userDetail;

        public UserDetailRepository(IOptions<MongoDbSettings> settings)
        {
            
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _userDetail = database.GetCollection<UserDetail>(settings.Value.UserDetailCollectionName);
        }



       



        public async Task<bool> DeleteUserDetailAsync(int userId)
        {
            var result = await _userDetail.DeleteOneAsync(u => u.UserId == userId);
            return result.DeletedCount > 0;
        }
        public async Task<bool> UpdateUserDetailAsync(int userId, UserDetail updatedUserDetail)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            var updateDefinitions = new List<UpdateDefinition<UserDetail>>();

            if (!string.IsNullOrEmpty(updatedUserDetail.Name))
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.Name, updatedUserDetail.Name));

            if (!string.IsNullOrEmpty(updatedUserDetail.Avatar))
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.Avatar, updatedUserDetail.Avatar));

            if (!string.IsNullOrEmpty(updatedUserDetail.PhoneNumber))
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.PhoneNumber, updatedUserDetail.PhoneNumber));

            if (!string.IsNullOrEmpty(updatedUserDetail.Gender))
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.Gender, updatedUserDetail.Gender));

            if (updatedUserDetail.Birthday != default)
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.Birthday, updatedUserDetail.Birthday));

            if (updatedUserDetail.Category != null)
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.Category, updatedUserDetail.Category));

            if (updatedUserDetail.Cart != null)
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.Cart, updatedUserDetail.Cart));

            if (updatedUserDetail.Wishlist != null)
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.Wishlist, updatedUserDetail.Wishlist));

            if (updatedUserDetail.ReceiveInfo != null)
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.ReceiveInfo, updatedUserDetail.ReceiveInfo));

            if (updatedUserDetail.Banking != null)
                updateDefinitions.Add(Builders<UserDetail>.Update.Set(u => u.Banking, updatedUserDetail.Banking));

            if (updateDefinitions.Count == 0)
                return false;

            var update = Builders<UserDetail>.Update.Combine(updateDefinitions);
            var result = await _userDetail.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }



        public async Task<bool> UpdateUserInfo(int userId,string name, string phoneNumber, string gender, DateTime birthDay)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<UserDetail>.Update
                .Set(u => u.Name, name)
                .Set(u => u.PhoneNumber, phoneNumber)
                .Set(u => u.Gender, gender)
                .Set(u => u.Birthday, birthDay);

            var result= await _userDetail.UpdateOneAsync(filter, update);
            return result.MatchedCount > 0 && result.ModifiedCount > 0;
        }
        public async Task<UserDetail> GetUserDetailAsync(int userId)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            return await _userDetail.Find(filter).FirstOrDefaultAsync();
        }
        public async Task<UserDetail> GetUserByUserId(int userId)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            return await _userDetail.Find(filter).FirstOrDefaultAsync();
        }
        public async Task AddUserDetailAsync(UserDetail user)
        {

            await _userDetail.InsertOneAsync(user);
        }

        public async Task<List<string>> GetCategoriesByPointDescending(int userId)
        {
            var user = await GetUserByUserId(userId);
            if (user?.Category == null)
                return new List<string>();

            return user.Category
                       .OrderByDescending(pair => pair.Value)
                       .Select(pair => pair.Key)
                       .ToList();
        }
        
        // WISHLIST



        public async Task EnsureWishlistFieldExists()
        {
            var filter = Builders<UserDetail>.Filter.Exists(u => u.Wishlist, false);
            var update = Builders<UserDetail>.Update.Set(u => u.Wishlist, new List<WishlistItem>());
            await _userDetail.UpdateManyAsync(filter, update);
        }

        public async Task<bool> IsProductInWishlistAsync(int userId, string productId)
        {
            // Find the user by UserId
            var userDetail = await _userDetail
                .Find(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            if (userDetail == null)
            {
                // User not found, return false
                return false;
            }

            // Check if the product exists in the wishlist
            var isProductInWishlist = userDetail.Wishlist.Any(w => w.ProductId == productId);

            return isProductInWishlist;
        }

        public async Task<bool> AddWishlistItemAsync(int userId, WishlistItem item)
        {
            // Prevent duplicate products in the wishlist
            var filter = Builders<UserDetail>.Filter.And(
                Builders<UserDetail>.Filter.Eq(u => u.UserId, userId),
                Builders<UserDetail>.Filter.Not(
                    Builders<UserDetail>.Filter.ElemMatch(u => u.Wishlist, w => w.ProductId == item.ProductId)
                )
            );

            var update = Builders<UserDetail>.Update.Push(u => u.Wishlist, item);
            var result = await _userDetail.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> RemoveWishlistItemAsync(int userId, string productId)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<UserDetail>.Update.PullFilter(u => u.Wishlist, w => w.ProductId == productId);
            var result = await _userDetail.UpdateOneAsync(filter, update);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> MoveWishlistItemToCartAsync(int userId, string productId)
        {
            // 1️⃣ Find user
            var user = await _userDetail.Find(u => u.UserId == userId).FirstOrDefaultAsync();
            if (user == null || user.Wishlist == null)
                return false;

            // 2️⃣ Find the wishlist item
            var wishlistItem = user.Wishlist.FirstOrDefault(w => w.ProductId == productId);
            if (wishlistItem == null)
                return false;

            // 3️⃣ Remove the item from wishlist
            var removeResult = await RemoveWishlistItemAsync(userId, productId);

            // 4️⃣ Check if item already exists in cart
            var existingCartItem = user.Cart?.FirstOrDefault(c => c.ProductId == productId);
            if (existingCartItem != null)
            {
                // If exists, just increment quantity by 1
                var filterUpdate = Builders<UserDetail>.Filter.And(
                    Builders<UserDetail>.Filter.Eq(u => u.UserId, userId),
                    Builders<UserDetail>.Filter.ElemMatch(u => u.Cart, c => c.ProductId == productId)
                );

                var updateQty = Builders<UserDetail>.Update.Inc("Cart.$.Quantity", 1);
                var updateResult = await _userDetail.UpdateOneAsync(filterUpdate, updateQty);
                return updateResult.ModifiedCount > 0;
            }
            else
            {
                // If not exists, add as a new cart item
                var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
                var newCartItem = new CartItem
                {
                    ProductId = wishlistItem.ProductId,
                    ProductName = wishlistItem.ProductName,
                    Image = wishlistItem.Image,
                    Quantity = 1, // default quantity
                    UnitPrice = wishlistItem.UnitPrice
                };

                var update = Builders<UserDetail>.Update.Push(u => u.Cart, newCartItem);
                var addResult = await _userDetail.UpdateOneAsync(filter, update);
                return removeResult && addResult.ModifiedCount > 0;
            }
        }







        // CART
        public async Task<bool> IsProductInCartAsync(int userId, string productId)
        {
            // Find the user by UserId
            var userDetail = await _userDetail
                .Find(u => u.UserId == userId)
                .FirstOrDefaultAsync();

            if (userDetail == null)
            {
                // User not found, return false
                return false;
            }

            // Check if the product exists in the cart
            var isProductInCart = userDetail.Cart.Any(c => c.ProductId == productId);

            return isProductInCart;
        }

        public async Task<bool> AddCartItemAsync(int userId, CartItem item)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            //var update = Builders<UserDetail>.Update.Push(u => u.Cart, item);
            //await _userDetail.UpdateOneAsync(filter, update);

            var user = await _userDetail.Find(filter).FirstOrDefaultAsync();
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var existingItem = user.Cart.FirstOrDefault(i => i.ProductId == item.ProductId);

            if (existingItem != null)
            {
                //// Update quantity (you can customize what "updating" means)
                //var update = Builders<UserDetail>.Update
                //    .Set(u => u.Cart[-1].Quantity, existingItem.Quantity + item.Quantity);

                //// Match user and the specific cart item by product ID
                //var arrayFilter = Builders<UserDetail>.Filter.And(
                //    filter,
                //    Builders<UserDetail>.Filter.ElemMatch(u => u.Cart, i => i.ProductId == item.ProductId)
                //);

                InsertCartItemQuantityAsync(userId, item.ProductId, existingItem.Quantity + item.Quantity);
                //await _userDetail.UpdateOneAsync(arrayFilter, update);
                return false;
            }
            else
            {
                // Add new cart item
                var update = Builders<UserDetail>.Update.Push(u => u.Cart, item);
                await _userDetail.UpdateOneAsync(filter, update);
                return true; // New item addedS
            }
        }


        public async Task<int> CountCartItems(int userId)
        {

            var user = await GetUserByUserId(userId); // Assuming synchronous call

            if (user == null || user.Cart == null)
                return 0;

            return user.Cart.Count; // Count of different CartItem entries
        }
        public async Task<List<CartItem>> GetCartItemsAsync(int userId)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);

            var projection = Builders<UserDetail>.Projection.Include(u => u.Cart);

            var result = await _userDetail.Find(filter).Project<UserDetail>(projection).FirstOrDefaultAsync();

            return result?.Cart ?? new List<CartItem>();
        }


        public async Task<int?> UpdateCartItemQuantityAsync(int userId, string productId, int changeAmount)
        {
            // Step 1: Get current quantity
            var user = await _userDetail.Find(
                Builders<UserDetail>.Filter.Eq(u => u.UserId, userId)
            ).FirstOrDefaultAsync();

            var item = user?.Cart?.FirstOrDefault(c => c.ProductId == productId);
            if (item == null) return null;

            int newQuantity = item.Quantity + changeAmount;

            // Step 2: Check if the new quantity would be <= 1
            if (newQuantity <= 0)
            {
                return item.Quantity; // Don't allow reducing to 1 or below
            }

            // Step 3: Proceed with update
            var filter = Builders<UserDetail>.Filter.And(
                Builders<UserDetail>.Filter.Eq(u => u.UserId, userId),
                Builders<UserDetail>.Filter.ElemMatch(u => u.Cart, c => c.ProductId == productId)
            );

            var update = Builders<UserDetail>.Update.Inc("Cart.$.Quantity", changeAmount);

            var result = await _userDetail.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                return newQuantity;
            }

            return null;
        }

        public async Task<int?> InsertCartItemQuantityAsync(int userId, string productId, int quantity)
        {
            // Step 1: Get current quantity
            var user = await _userDetail.Find(
                Builders<UserDetail>.Filter.Eq(u => u.UserId, userId)
            ).FirstOrDefaultAsync();

            var item = user?.Cart?.FirstOrDefault(c => c.ProductId == productId);
            if (item == null) return null;

       

            // Step 2: Check if the new quantity would be <= 1
            if (quantity <= 0)
            {
                return item.Quantity; // Don't allow reducing to 1 or below
            }

            // Step 3: Proceed with update
            var filter = Builders<UserDetail>.Filter.And(
                Builders<UserDetail>.Filter.Eq(u => u.UserId, userId),
                Builders<UserDetail>.Filter.ElemMatch(u => u.Cart, c => c.ProductId == productId)
            );

            var update = Builders<UserDetail>.Update.Set("Cart.$.Quantity", quantity);/*.Inc("Cart.$.Quantity", changeAmount);*/

            var result = await _userDetail.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                return quantity;
            }

            return null;
        }

        public async Task RemoveCartItemAsync(int userId, string productId)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<UserDetail>.Update.PullFilter(
                u => u.Cart,
                c => c.ProductId == productId
            );
            await _userDetail.UpdateOneAsync(filter, update);
        }








        //RECEIVE INFO
        public async Task<List<ReceiveInfo>> GetReceiveInfoAsync(int userId)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            var projection = Builders<UserDetail>.Projection.Include(u => u.ReceiveInfo).Exclude("_id");
            var result = await _userDetail.Find(filter).Project<UserDetail>(projection).FirstOrDefaultAsync();
            return result?.ReceiveInfo ?? new List<ReceiveInfo>();
        }

        public async Task AddReceiveInfoAsync(int userId, ReceiveInfo newInfo)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<UserDetail>.Update.Push("ReceiveInfo", newInfo); // string path works too

            await _userDetail.UpdateOneAsync(filter, update);
        }

        public async Task<bool> DeleteReceiveInfoAsync(int userId, ReceiveInfo targetInfo)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);

            var nestedFilter = Builders<ReceiveInfo>.Filter.And(
                Builders<ReceiveInfo>.Filter.Eq(r => r.Name, targetInfo.Name),
                Builders<ReceiveInfo>.Filter.Eq(r => r.Phone, targetInfo.Phone),
                Builders<ReceiveInfo>.Filter.Eq(r => r.Address, targetInfo.Address)
            );

            // Use "ReceiveInfo" as a string field path
            var update = Builders<UserDetail>.Update.PullFilter("ReceiveInfo", nestedFilter);

            var result = await _userDetail.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task UpdateBankingInfoAsync(int userId, string newAccount, string newCard)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);

            var update = Builders<UserDetail>.Update
                .Set(u => u.Banking.BankAccount, newAccount)
                .Set(u => u.Banking.CreditCard, newCard);

            await _userDetail.UpdateOneAsync(filter, update);
        }

        public async Task UpdatePhoneNumberAsync(int userId, string newPhoneNumber)
        {
            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<UserDetail>.Update.Set(u => u.PhoneNumber, newPhoneNumber);
            await _userDetail.UpdateOneAsync(filter, update);
        }









        public async Task InsertUserCategoriesAsync(int userId, List<string> categoryNames)
        {
            var categoryDict = categoryNames.ToDictionary(name => name, name => 0);

            var filter = Builders<UserDetail>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<UserDetail>.Update.Set(u => u.Category, categoryDict);

            await _userDetail.UpdateOneAsync(filter, update);
        }

        public async Task UpdateCategoryScoreAsync(int userId, string categoryName, UserAction action)
        {
            int points = action switch
            {
                UserAction.Click => 50,
                UserAction.AddToCart => 100,
                UserAction.Purchase => 200,
                UserAction.ClickCategory => 100,
                _ => 0
            };

            var user = await _userDetail.Find(u => u.UserId == userId).FirstOrDefaultAsync();
            if (user == null) throw new Exception("User not found");

            
            // Ensure Reset key exists
            if (!user.Category.ContainsKey("Reset"))
                user.Category["Reset"] = 0;

            // Ensure category key exists
            if (!user.Category.ContainsKey(categoryName))
                user.Category[categoryName] = 0;

            // Update selected category
            user.Category[categoryName] += points;

            // Update Reset
            user.Category["Reset"] += points;

            // --- Check if Reset threshold reached ---
            if (user.Category["Reset"] > 200)
            {
                foreach (var key in user.Category.Keys.ToList())
                {
                    if (key == "Reset") continue;
                    user.Category[key] = (int)Math.Floor(user.Category[key] * 0.8); // decrease by 20%
                }

                user.Category["Reset"] = 0;
            }

            // --- Bonus rule ---
            // Get max score excluding Reset (giúp những tìm kiếm mới được hiện nhiều hơn)
            var maxValue = user.Category
                .Where(kv => kv.Key != "Reset")
                .Select(kv => kv.Value)
                .DefaultIfEmpty(0)
                .Max();

            int current = user.Category[categoryName];

            if (categoryName != "Reset" && current < maxValue /*/ 2*/)
            {
                int bonus = (int)Math.Floor(maxValue * 0.25); // 25% of highest
                user.Category[categoryName] += bonus;
            }

            // Save back to DB
            var update = Builders<UserDetail>.Update.Set(u => u.Category, user.Category);
            await _userDetail.UpdateOneAsync(u => u.UserId == userId, update);
        }
    }
}
