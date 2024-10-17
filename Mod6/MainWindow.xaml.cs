using Npgsql;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Mod6
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Host=localhost;Username=postgres;Password=Admin;Port=5432;Database=library";
        public ObservableCollection<Book> Books { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Books = new ObservableCollection<Book>();
            BooksDataGrid.ItemsSource = Books;
        }

        // Поиск книг
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchQuery = SearchTextBox.Text;
            SearchBooks(searchQuery);
        }

        private void SearchBooks(string searchQuery)
        {
            Books.Clear();
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    var command = new NpgsqlCommand("SELECT id, title, author, available FROM books WHERE title ILIKE @query OR author ILIKE @query", conn);
                    command.Parameters.AddWithValue("query", "%" + searchQuery + "%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Books.Add(new Book
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Author = reader.GetString(2),
                                Available = reader.GetString(3).ToLower() == "true" // Преобразование строки в bool
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Аренда книги
        private void RentButton_Click(object sender, RoutedEventArgs e)
        {
            if (BooksDataGrid.SelectedItem is Book selectedBook)
                RentBook(selectedBook.Id);
        }

        private void RentBook(int bookId)
        {
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                // Проверим доступность книги
                var checkCommand = new NpgsqlCommand("SELECT available FROM books WHERE id = @bookId", conn);
                checkCommand.Parameters.AddWithValue("bookId", bookId);

                var isAvailableString = checkCommand.ExecuteScalar() as string;
                var isAvailable = isAvailableString?.ToLower() == "true";

                if (isAvailable)
                {
                    var transaction = conn.BeginTransaction();

                    try
                    {
                        // Помечаем книгу как недоступную (обновляем колонку "available")
                        var updateCommand = new NpgsqlCommand("UPDATE books SET available = 'false' WHERE id = @bookId", conn);
                        updateCommand.Parameters.AddWithValue("bookId", bookId);
                        updateCommand.ExecuteNonQuery();

                        transaction.Commit();
                        MessageBox.Show("Вы арендовали книгу.");

                        // Обновляем список книг после аренды
                        SearchBooks(SearchTextBox.Text);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
                else
                    MessageBox.Show("Книга недоступна.");
            }
        }

        // Поиск при нажатии клавиши Enter
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchBooks(SearchTextBox.Text);
        }
    }

    // Модель книги
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public bool Available { get; set; }
    }
}