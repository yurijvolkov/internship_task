using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yandex_Internship.Domain;
using System.IO;

namespace Yandex_Internship
{
    static class Parser
    {
        /// <summary>
        /// Parsing input file and returns list of Order entities and list of error logs
        /// </summary>
        /// <param name="fileName">Path to input file</param>
        /// <param name="progress">Used for report progress (for more info look at ConsoleProgress summary)</param>
        /// <returns>Tuple ( Item1 : list of correct orders, Item2 : list of error logs (pairs of line number and short explanation))</returns>
        internal static async Task<Tuple<List<Order>, List<KeyValuePair<int, string>>>> ParseFileAsync(string fileName, IProgress<Tuple<long, long, bool>> progress)
        {
            List<Order> orders = new List<Order>(); //successful parsed orders
            List<KeyValuePair<int, string>> errorLogs =
                new List<KeyValuePair<int, string>>(); //indexes of unseccessful parsed orders and explanasion
            int currentLine = 0; //current processing line

            StreamReader reader = new StreamReader(new FileStream(fileName, FileMode.Open));

            string[] header = (await reader.ReadLineAsync().ConfigureAwait(false)).Split('\t');
            Dictionary<string, int> headerOrder = new Dictionary<string, int> //order of columns in input file
            {
                {header[0], 0},
                {header[1], 1},
                {header[2], 2},
                {header[3], 3}
            };

            while (!reader.EndOfStream)
            {
                progress.Report(new Tuple<long, long, bool>(currentLine++, -1, true));

                string data = await reader.ReadLineAsync().ConfigureAwait(false);
                string[] splitted = data.Split('\t');
                try
                {
                    int id, productId;
                    double amount;
                    DateTime dateTime;

                    if (!int.TryParse(splitted[headerOrder["id"]], out id))
                    {
                        errorLogs.Add(new KeyValuePair<int, string>(currentLine, "Incorrect 'id' value"));
                        continue;
                    }
                    if (!int.TryParse(splitted[headerOrder["product_id"]], out productId) || productId > 7 || productId < 1)
                    {
                        errorLogs.Add(new KeyValuePair<int, string>(currentLine, "Incorrect 'product_id'"));
                        continue;
                    }
                    if (!double.TryParse(splitted[headerOrder["amount"]], out amount) || amount < 0)
                    {
                        errorLogs.Add(new KeyValuePair<int, string>(currentLine, "Incorrect 'amount'"));
                        continue;
                    }
                    if (!DateTime.TryParse(splitted[headerOrder["dt"]], out dateTime))
                    {
                        errorLogs.Add(new KeyValuePair<int, string>(currentLine, "Incorrect 'dt' value"));
                        continue;
                    }
                    orders.Add(new Order(id, dateTime, productId, amount));
                }
                catch (IndexOutOfRangeException)
                {
                    errorLogs.Add(new KeyValuePair<int, string>(currentLine, "Not enough values"));
                }
            }
            reader.Close();
            return new Tuple<List<Order>, List<KeyValuePair<int, string>>>(orders, errorLogs);
        }

    }
}
