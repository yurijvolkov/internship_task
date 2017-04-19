using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using Yandex_Internship.Domain;

namespace Yandex_Internship
{
    /// <summary>
    /// Static class of methods for interacting with SQLite database
    /// </summary>
    static class ApiDb
    {
        /// <summary>
        /// Initializing database
        /// </summary>
        /// <param name="progress">Used for report progress (for more info look at ConsoleProgress summary)</param>
        /// <returns></returns>
        internal static async Task InitDbAsync(IProgress<Tuple<long, long, bool>> progress)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["dbConnection"].ConnectionString; //defined in App.config
            
            var initCommand = "DROP TABLE IF EXISTS 'order';" +
                              "DROP TABLE IF EXISTS product;" +
                              "CREATE TABLE product( id INTEGER PRIMARY KEY, name TEXT);" +
                              "CREATE TABLE 'order'( id INTEGER PRIMARY KEY, dt TEXT, product_id INTEGER, amount REAL," +
                              "FOREIGN KEY(product_id) REFERENCES product(id));";
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                var command = new SQLiteCommand(initCommand, connection);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            
            var products = new List<Product>(
                new[]{
                    new Product(1, "A"),
                    new Product(2, "B"),
                    new Product(2, "C"),
                    new Product(4, "D"),
                    new Product(5, "E"),
                    new Product(6, "F"),
                    new Product(7, "G")
                });

            await AddProducts(products, new ConsoleProgress("В базу добавляются товары (product)")); //preloading content of 'product' table (as said in task)

            progress.Report(new Tuple<long, long, bool>(1, -1, true));
        }
        
        /// <summary>
        /// Adding products to DB with help of transactions
        /// </summary>
        /// <param name="products">List of Product entities</param>
        /// <param name="progress">Used for report progress (for more info look at ConsoleProgress summary)</param>
        /// <returns></returns>
        internal static Task AddProducts(List<Product> products, IProgress<Tuple<long, long, bool>> progress)
        {
            return Task.Run(() =>
            {
                var connectionString = ConfigurationManager.ConnectionStrings["dbConnection"].ConnectionString;//defined in App.config
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand(connection))
                    using (var transaction = connection.BeginTransaction())
                    {
                        int c = 0;
                        foreach (var product in products)
                        {
                            cmd.CommandText = "INSERT INTO product (name)" + $"Values ('{product.Name}')";
                            cmd.ExecuteNonQuery();
                            progress.Report(new Tuple<long, long, bool>(c++, products.Count, true));
                        }
                        transaction.Commit();
                    }
                }
            });
        }

        /// <summary>
        /// Adding orders to DB with help of transactions
        /// </summary>
        /// <param name="orders">List of Order entities</param>
        /// <param name="progress">Used for report progress (for more info look at ConsoleProgress summary)</param>
        /// <returns></returns>
        internal static Task AddOrders(List<Order> orders, IProgress<Tuple<long, long, bool>> progress)
        {
            return Task.Run(() =>
            {
                var connectionString = ConfigurationManager.ConnectionStrings["dbConnection"].ConnectionString; //defined in App.config
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand(connection))
                    using (var transaction = connection.BeginTransaction())
                    {
                        int c = 0;
                        foreach (var order in orders)
                        {
                            cmd.CommandText = "INSERT INTO 'order' (dt, product_id, amount)" +
                                              $"Values ('{order.DateTime}', '{order.ProductId}', '{order.Amount}')";
                            cmd.ExecuteNonQuery();
                            progress.Report(new Tuple<long, long, bool>(c++, orders.Count, true));
                        }
                        transaction.Commit();
                    }
                }

            });
        }

        /// <summary>
        /// Perform all requests to database
        /// </summary>
        /// <param name="progress">Used for report progress (for more info look at ConsoleProgress summary)</param>
        /// <returns>dynamic object with all responses</returns>
        internal static Task<object> MakeRequests(IProgress<Tuple<long, long, bool>> progress)
        {
            return Task.Run<object>(() =>
            {
                var connectionString = ConfigurationManager.ConnectionStrings["dbConnection"].ConnectionString; //defined in App.config
                var sqlExpr = "SELECT * FROM product; SELECT * FROM 'order';";

                progress.Report(new Tuple<long, long, bool>(0, -1, false));

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var adapter = new SQLiteDataAdapter(sqlExpr, connection);
                    var dataSet = new DataSet();

                    adapter.Fill(dataSet);
                    var productTable = dataSet.Tables[0].AsEnumerable().ToList();
                    var orderTable = dataSet.Tables[1].AsEnumerable().ToList(); 

                    //response for first request
                    var result1 = orderTable
                        .Where(x =>
                        {
                            var date = DateTime.Parse((string)x["dt"]);
                            return date.Year == DateTime.Now.Year && date.Month == DateTime.Now.Month; //filtering  orders that were performed in current month
                        })
                        .GroupBy(x => x["product_id"])
                        .Select(g => new
                        {
                            Name = productTable.FirstOrDefault(x => (long)x["id"] == (long)g.Key)["name"], //transforming product_id(g.Key) to it's name
                            Sum = g.Sum(x => (double)x["amount"]),
                            Count = g.Count()
                        }).ToList();

                    //adding products that didn't appear in current month
                    foreach (var dataRow in productTable)
                        if (result1.Count(x => x.Name == dataRow["name"]) == 0)
                            result1.Add(new { Name = dataRow["name"], Sum = 0.0, Count = 0 });
                    
                    //response for 2.a. request
                    var result2_1 = orderTable
                        .GroupBy(x => x["product_id"])
                        .Where(g => g.Count(row =>
                        {
                            var curDate = DateTime.Parse((string)row["dt"]);
                            return curDate.Month != DateTime.Now.Month   
                                   || curDate.Year != DateTime.Now.Year; //filtering products that were sold only in current month
                        }) == 0)
                        .Select(g => productTable.FirstOrDefault(d => (long)d["id"] == (long)g.Key)["name"]); //transforming product_id to it's name

                    //response for first part of 2.b. request
                    var result2_2_1 = orderTable
                        .GroupBy(x => x["product_id"])
                        .Where(g => g.Count(row =>
                        {
                            var curDate = DateTime.Parse((string)row["dt"]);
                            return curDate.Month == DateTime.Now.Month
                                   && curDate.Year == DateTime.Now.Year;  //filtering products that were NOT sold in current month
                        }) == 0)
                        .Where(g => g.Count(row =>
                        {
                            var curDate = DateTime.Parse((string)row["dt"]);
                            return curDate.Month == (DateTime.Now.Month + 11) % 12
                                   && curDate.Year == DateTime.Now.Year;  //filtering products that were sold in previous month
                        }) != 0)
                        .Select(g => productTable.FirstOrDefault(d => (long)d["id"] == (long)g.Key)["name"]); //transforming product_id to it's name

                    //response for second part of 2.b. request
                    var result2_2_2 = orderTable
                        .GroupBy(x => x["product_id"])
                        .Where(g => g.Count(row =>
                        {
                            var curDate = DateTime.Parse((string)row["dt"]);
                            return curDate.Month == (DateTime.Now.Month + 11) % 12
                                   && curDate.Year == DateTime.Now.Year; //filtering products that were NOT sold in previous month
                        }) == 0) 
                        .Where(g => g.Count(row =>
                        {
                            var curDate = DateTime.Parse((string)row["dt"]);
                            return curDate.Month == DateTime.Now.Month
                                   && curDate.Year == DateTime.Now.Year; //filtering products that were sold in current month
                        }) != 0)
                        .Select(g => productTable.FirstOrDefault(d => (long)d["id"] == (long)g.Key)["name"]); //transforming product_id to it's name

                    //response for third request
                    var result3 = orderTable
                        .GroupBy(x =>
                        {
                            var dt = DateTime.Parse((string)x["dt"]);
                            return dt.Month.ToString() + " " + dt.Year.ToString();
                        })
                        .Select(g =>
                        {
                            var maxGroup = g.GroupBy(d => d["product_id"])
                                .Aggregate(
                                    (g1, g2) => g1.Sum(d => (double)d["amount"]) >
                                                g2.Sum(d => (double)d["amount"])
                                        ? g1
                                        : g2);
                            var totalSum = g.Sum(d => (double)d["amount"]);

                            return new
                            {
                                Month = g.Key,
                                Value = maxGroup.Sum(d => (double)d["amount"]),
                                Name = productTable.FirstOrDefault(row => (long)row["id"] == (long)maxGroup.Key)["name"],
                                Part = maxGroup.Sum(d => (double)d["amount"]) / totalSum
                            };
                        }).OrderBy( x=> x.Month.Split(' ')[1] + x.Month.Split(' ')[0]);


                    return (dynamic)new
                    {
                        Req1 = result1,
                        Req2 = new
                        {
                            Req1 = result2_1,
                            Req2 = new
                            {
                                Req1 = result2_2_1,
                                Req2 = result2_2_2
                            }
                        },
                        Req3 = result3
                    };
                }
            });
        }
    }
}
