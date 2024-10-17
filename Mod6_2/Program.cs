using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mod6_2
{
    class Program
    {
        private static FirebaseClient firebaseClient;

        static async Task Main(string[] args)
        {
            // Инициализация Firebase
            firebaseClient = new FirebaseClient("https://library-93792-default-rtdb.firebaseio.com/");

            // Путь к вашему файлу books.sql
            string filePath = "C:/Users/Артём/Downloads/books.sql";

            // Чтение файла и извлечение данных
            string[] lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                // Пример: insert into books (title, author, available) values ('The Cave of the Golden Rose', 'Lucretia Bearman', true);
                var match = Regex.Match(line, @"insert into books \(title, author, available\) values \('([^']+)', '([^']+)', (\w+)\);");
                if (match.Success)
                {
                    string title = match.Groups[1].Value;
                    string author = match.Groups[2].Value;
                    bool available = bool.Parse(match.Groups[3].Value.ToLower());

                    // Создание книги
                    var book = new Book
                    {
                        Title = title,
                        Author = author,
                        Available = available
                    };

                    // Генерация уникального ID (можно использовать GUID или инкремент)
                    var id = Guid.NewGuid();

                    // Загрузка книги в Firebase
                    await firebaseClient
                        .Child("books")
                        .Child(id.ToString())
                        .PutAsync(book);
                }
            }

            Console.WriteLine("Data imported successfully!");
        }
    }

    public class Book
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public bool Available { get; set; }
    }
}