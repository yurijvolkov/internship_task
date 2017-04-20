using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace Yandex_Internship
{
    /// <summary>
    /// Used to show progress from async tasks
    /// </summary>
    class ConsoleProgress : IProgress<Tuple<long, long, bool>>
    {
        public ConsoleProgress(string message)
        {
            Message = message;
        }
        
        /// <summary>
        /// Reporting progress every 5e5 progress stamps
        /// </summary>
        /// <param name="progress">Tuple values : (Item1 : current progress, Item2 : total progress, Item3 : clear Console or not) </param>
        public void Report(Tuple<long, long, bool> progress)
        {
            if (progress.Item1 % 50000 == 0)
            {
                if (progress.Item3)
                    Console.Clear();
                Console.WriteLine($"{Message}  {(!progress.Item3 ? "" : progress.Item1.ToString())}  {(progress.Item2 == -1 ? "" : "/ " + progress.Item2)}");
            }

        }

        /// <summary>
        /// Text displayed before report
        /// </summary>
        string Message { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            return AsyncContext.Run(() => MainAsync(args));
        }

        static async Task<int> MainAsync(string[] args)
        {
            try
            {
                var parsingFileTask = Parser.ParseFileAsync(args[0], new ConsoleProgress("Парсинг строк")); 
                await ApiDb.InitDbAsync(new ConsoleProgress("Инициализация бд")); //creating tables, filling products table, etc 

                var parsingFile = await parsingFileTask;
                await ApiDb.AddOrdersAsync(parsingFile.Item1, new ConsoleProgress("В базу добавляются заказы (order)"));

                Console.Clear();
                DisplayErrorLines(parsingFile.Item2);
                DisplayResponse(await ApiDb.MakeRequests(new ConsoleProgress("Выполнение запросов.")));
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Ошибка : Неверное имя файла. ");
                return 1;
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine($"Ошибка : Не указано имя файла.");
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Произошла непредвиденная ошибка.\n{e.Message}");
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Displaying numbers of lines that were failed to parse
        /// </summary>
        /// <param name="errorLogs">Key : number of line, Value : some explanation about fail</param>
        static void DisplayErrorLines(List<KeyValuePair<int, string>> errorLogs)
        {
            if (errorLogs.Count == 0)
                return;

            Console.WriteLine("Строки с ошибками :");
            foreach (var log in errorLogs)
                Console.WriteLine($"\tСтрока {log.Key} : {log.Value}");
        }

        /// <summary>
        /// Displaying results of all requests in human readable format
        /// </summary>
        /// <param name="response">dynamic object with all responses</param>
        static void DisplayResponse(dynamic response)
        {
            Console.WriteLine("Запрос №1 :");
            Console.WriteLine($"\t{"Продукт",-10} {"Кол-во заказов",-15} {"Общая сумма",-12}");
            foreach (var res in response.Req1)
                Console.WriteLine($"\t{res.Name,-10} {res.Count,-15} {res.Sum,-12: 0}");

            Console.WriteLine("\nЗапрос №2 :");
            Console.WriteLine("\ta. Продукты впервые заказанные в этом месяце :");
            Console.Write("\t\t");
            if (((IEnumerable<object>)response.Req2.Req1).Count() == 0)
                Console.WriteLine("(empty)");
            foreach (var res in response.Req2.Req1)
                Console.Write($"{res} ");

            Console.WriteLine("\n\tб.");
            Console.WriteLine("\t\tПродукты заказанные в прошлом месяце и не заказанные в этом месяце :");
            Console.Write("\t\t\t");
            if (((IEnumerable<object>)response.Req2.Req2.Req1).Count() == 0)
                Console.WriteLine("(empty)");
            foreach (var res in response.Req2.Req2.Req1)
                Console.Write($"{res} ");
            
            Console.WriteLine("\n\t\tПродукты заказанные в этом месяце и не заказанные в прошлом месяце: ");
            Console.Write("\t\t\t");
            if (((IEnumerable<object>)response.Req2.Req2.Req2).Count() == 0)
                Console.WriteLine("(empty)");
            foreach (var res in response.Req2.Req2.Req2)
                Console.Write($"{res} ");

            Console.WriteLine("\n\nЗапрос №3 :");
            Console.WriteLine($"\t{"Период",-12} {"Продукт",-12} {"Сумма",-12} {"Доля",-12}");
            foreach (var res in response.Req3)
                Console.WriteLine($"\t{res.Month,-12} {res.Name,-12} {res.Value,-12: 0} {100 * res.Part,-12: 0}");
        }
    }
}
