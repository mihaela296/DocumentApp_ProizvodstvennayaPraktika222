using DocumentApp_ProizvodstvennayaPraktika.Pages; // Или где у вас находится LawyerBasePage
using System;
using System.Collections.Generic;
using System.Data.Entity; // Если используете Entity Framework
using System.Data.Entity.Validation; // Для Entity Framework 6.x
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DocumentApp_ProizvodstvennayaPraktika
{
    /// <summary>
    /// Логика взаимодействия для ClientManagementWindow.xaml
    /// </summary>
    public partial class ClientManagementWindow : Window
    {
        public ClientManagementWindow()
        {
            InitializeComponent();
            LoadClients();
        }

        private void LoadClients()
        {
            try
            {
                using (var context = new Entities())
                {
                    var clients = context.Users
                        .Where(u => u.RoleId == 1) // Только клиенты
                        .Where(u => !string.IsNullOrEmpty(u.FullName) &&
                               !string.IsNullOrEmpty(u.Email) &&
                               u.CreatedAt != null) // Только заполненные записи
                        .OrderBy(u => u.UserId)
                        .ToList();

                    ClientsGrid.ItemsSource = clients;
                    ClientsGrid.CanUserAddRows = false; // Дополнительное отключение
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}");
                ClientsGrid.ItemsSource = new List<Users>();
            }
        }

        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            var editor = new ClientEditorWindow(new Users
            {
                RoleId = 1,
                CreatedAt = DateTime.Now,
                IsActive = true // Добавляем обязательное поле
            });

            if (editor.ShowDialog() == true)
            {
                try
                {
                    using (var context = new Entities())
                    {
                        // Проверяем обязательные поля
                        if (string.IsNullOrEmpty(editor.Client.FullName) ||
                            string.IsNullOrEmpty(editor.Client.Email) ||
                            string.IsNullOrEmpty(editor.Client.Username))
                        {
                            MessageBox.Show("Заполните все обязательные поля", "Ошибка");
                            return;
                        }

                        // Хэшируем пароль
                        if (!string.IsNullOrEmpty(editor.PasswordBox.Password))
                        {
                            editor.Client.PasswordHash = PasswordHasher.CreateHash(
                                editor.PasswordBox.Password, out string salt);
                            editor.Client.PasswordSalt = salt;
                        }
                        else
                        {
                            MessageBox.Show("Пароль не может быть пустым", "Ошибка");
                            return;
                        }

                        context.Users.Add(editor.Client);
                        context.SaveChanges();
                        LoadClients();
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                    MessageBox.Show("Ошибки валидации:\n" + string.Join("\n", errorMessages),
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                }
            }
        }

        private void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsGrid.SelectedItem is Users client)
            {
                var editor = new ClientEditorWindow(client);
                if (editor.ShowDialog() == true)
                {
                    try
                    {
                        using (var context = new Entities())
                        {
                            // Загружаем клиента из базы для обновления
                            var dbClient = context.Users.Find(client.UserId);
                            if (dbClient != null)
                            {
                                // Обновляем поля
                                dbClient.FullName = editor.Client.FullName;
                                dbClient.Email = editor.Client.Email;
                                dbClient.Phone = editor.Client.Phone;
                                dbClient.Username = editor.Client.Username;

                                // Если пароль был изменен, хэшируем его
                                if (!string.IsNullOrEmpty(editor.PasswordBox.Password))
                                {
                                    dbClient.PasswordHash = PasswordHasher.CreateHash(
                                        editor.PasswordBox.Password, out string salt);
                                    dbClient.PasswordSalt = salt;
                                }

                                context.SaveChanges();
                                LoadClients(); // Обновляем список
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsGrid.SelectedItem is Users selectedClient)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить клиента {selectedClient.FullName}?",
                                           "Подтверждение удаления",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new Entities())
                        {
                            // Проверяем, есть ли у клиента договоры
                            bool hasContracts = context.ClientContracts
                                .Any(c => c.ClientId == selectedClient.UserId);

                            if (hasContracts)
                            {
                                MessageBox.Show("Нельзя удалить клиента с существующими договорами. Сначала удалите все договоры клиента.",
                                              "Ошибка",
                                              MessageBoxButton.OK,
                                              MessageBoxImage.Error);
                                return;
                            }

                            // Удаляем клиента
                            var clientToDelete = context.Users.Find(selectedClient.UserId);
                            if (clientToDelete != null)
                            {
                                context.Users.Remove(clientToDelete);
                                context.SaveChanges();
                                LoadClients(); // Обновляем список
                                MessageBox.Show("Клиент успешно удален", "Успех");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении клиента: {ex.Message}", "Ошибка");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите клиента для удаления", "Ошибка");
            }
        }

        // Добавляем обработчик поиска
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                LoadClients();
                return;
            }

            var searchText = SearchBox.Text.ToLower();
            using (var context = new Entities())
            {
                ClientsGrid.ItemsSource = context.Users
                    .Where(u => u.RoleId == 1 &&
                           (u.FullName.ToLower().Contains(searchText) ||
                            u.Email.ToLower().Contains(searchText)))
                    .ToList();
            }
        }

        private void ViewContracts_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsGrid.SelectedItem is Users client)
            {
                var window = new ClientContractsWindow(client.UserId);
                window.ShowDialog();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Вариант 1: Если LawyerBasePage - это Page и используется NavigationWindow
                if (Application.Current.MainWindow is NavigationWindow navWindow)
                {
                    navWindow.Navigate(new LawyerBasePage());
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переходе: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
