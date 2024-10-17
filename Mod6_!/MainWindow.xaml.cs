using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Mod6_1
{
    public partial class MainWindow : Window
    {
        private FirebaseClient firebaseClient;
        public ObservableCollection<Book> Books { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Books = new ObservableCollection<Book>();
            BooksDataGrid.ItemsSource = Books;

            firebaseClient = new FirebaseClient("https://library-93792-default-rtdb.firebaseio.com/");

            LoadBooks();
        }

        // Загрузка книг из Firebase
        private async void LoadBooks()
        {
            Books.Clear();
            try
            {
                var books = await firebaseClient
                    .Child("books")
                    .OnceAsync<Book>();

                foreach (var book in books)
                {
                    // Важно сохранять уникальный ключ Firebase для каждой книги
                    Books.Add(new Book
                    {
                        Id = book.Object.Id,
                        Title = book.Object.Title,
                        Author = book.Object.Author,
                        Available = book.Object.Available,
                        FirebaseKey = book.Key // Сохраняем ключ Firebase
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке книг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Поиск книг
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchQuery = SearchTextBox.Text;
            await SearchBooks(searchQuery);
        }

        private async Task SearchBooks(string searchQuery)
        {
            Books.Clear();
            try
            {
                var books = await firebaseClient
                    .Child("books")
                    .OnceAsync<Book>();

                foreach (var book in books)
                {
                    var bookObj = book.Object;

                    if (bookObj.Title.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        bookObj.Author.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Books.Add(new Book
                        {
                            Id = bookObj.Id,
                            Title = bookObj.Title,
                            Author = bookObj.Author,
                            Available = bookObj.Available,
                            FirebaseKey = book.Key // Сохраняем ключ Firebase
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Аренда книги
        private async void RentButton_Click(object sender, RoutedEventArgs e)
        {
            if (BooksDataGrid.SelectedItem is Book selectedBook)
                await RentBook(selectedBook);
        }

        private async Task RentBook(Book book)
        {
            if (book.Available)
            {
                book.Available = false;

                try
                {
                    // Обновляем книгу по её уникальному ключу Firebase
                    await firebaseClient
                        .Child("books")
                        .Child(book.FirebaseKey)  // Используем ключ Firebase вместо Id
                        .PutAsync(book);

                    MessageBox.Show("Вы арендовали книгу.");

                    // Обновляем список книг после аренды
                    await SearchBooks(SearchTextBox.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
            else
                MessageBox.Show("Книга недоступна.");
        }

        // Поиск при нажатии клавиши Enter
        private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await SearchBooks(SearchTextBox.Text);
        }

        // Кнопка "Вернуть книгу"
        private async void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            if (BooksDataGrid.SelectedItem is Book selectedBook)
                await ReturnBook(selectedBook);
        }

        // Метод для возврата книги
        private async Task ReturnBook(Book book)
        {
            if (!book.Available)  // Книга уже арендована, поэтому доступность false
            {
                book.Available = true;  // Меняем на true, чтобы сделать её доступной

                try
                {
                    // Обновляем книгу по её уникальному ключу Firebase
                    await firebaseClient
                        .Child("books")
                        .Child(book.FirebaseKey)  // Используем ключ Firebase
                        .PutAsync(book);

                    MessageBox.Show("Книга возвращена.");

                    // Обновляем список книг после возврата
                    await SearchBooks(SearchTextBox.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
            else
                MessageBox.Show("Эта книга уже доступна.");
        }
    }

    // Модель книги
    public class Book
    {
        public int Id { get; set; }  // Предполагается, что ID у книги уже существует
        public string Title { get; set; }
        public string Author { get; set; }
        public bool Available { get; set; }
        public string FirebaseKey { get; set; }  // Новый параметр для хранения уникального ключа Firebase
    }
}
