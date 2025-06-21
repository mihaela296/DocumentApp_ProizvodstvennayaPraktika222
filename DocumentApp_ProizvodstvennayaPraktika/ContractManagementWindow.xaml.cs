using DocumentApp_ProizvodstvennayaPraktika.Pages;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace DocumentApp_ProizvodstvennayaPraktika
{
    public partial class ContractManagementWindow : Window
    {
        public ContractManagementWindow()
        {
            InitializeComponent();
            InitializeStatusFilter();
            LoadContracts();
        }

        private void InitializeStatusFilter()
        {
            try
            {
                StatusFilter.Items.Clear();
                StatusFilter.Items.Add(new ComboBoxItem { Content = "Все договоры", Tag = null });
                StatusFilter.Items.Add(new ComboBoxItem { Content = "Активные", Tag = "Active" });
                StatusFilter.Items.Add(new ComboBoxItem { Content = "Завершенные", Tag = "Completed" });
                StatusFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации фильтра: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadContracts()
        {
            try
            {
                using (var context = new Entities())
                {
                    var contracts = context.ClientContracts
                        .Include(c => c.ContractTemplates)
                        .Include(c => c.Users)
                        .OrderByDescending(c => c.ContractDate)
                        .AsNoTracking()
                        .ToList();

                    ContractsGrid.ItemsSource = contracts ?? new List<ClientContracts>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки договоров: {ex.Message}\n\n{ex.InnerException?.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(ContractsGrid.SelectedItem is ClientContracts selectedContract))
                {
                    MessageBox.Show("Выберите договор для просмотра", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (var context = new Entities())
                {
                    var fullContract = context.ClientContracts
                        .Include(c => c.ContractTemplates)
                        .Include(c => c.Users)
                        .AsNoTracking()
                        .FirstOrDefault(c => c.ContractId == selectedContract.ContractId);

                    if (fullContract == null)
                    {
                        MessageBox.Show("Договор не найден в базе данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var viewWindow = new ContractViewWindow(fullContract, fullContract.Users);
                    viewWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия договора: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                // 1. Получаем параметры фильтрации с защитой от null
                string searchText = SearchBox?.Text?.Trim()?.ToLower() ?? string.Empty;
                string statusFilter = (StatusFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString();

                Debug.WriteLine($"Фильтрация: searchText='{searchText}', statusFilter='{statusFilter}'");

                using (var context = new Entities())
                {
                    // 2. Базовый запрос с Include
                    IQueryable<ClientContracts> query = context.ClientContracts
                        .Include(c => c.ContractTemplates)
                        .Include(c => c.Users);

                    // 3. Безопасная фильтрация по тексту
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query = query.Where(c =>
                            (c.ContractTemplates != null &&
                             c.ContractTemplates.TemplateName != null &&
                             c.ContractTemplates.TemplateName.ToLower().Contains(searchText)) ||
                            (c.Users != null &&
                             c.Users.FullName != null &&
                             c.Users.FullName.ToLower().Contains(searchText)));

                        Debug.WriteLine("Фильтр по тексту применен");
                    }

                    // 4. Безопасная фильтрация по статусу
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        query = query.Where(c =>
                            c.Status != null &&
                            c.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));

                        Debug.WriteLine("Фильтр по статусу применен");
                    }

                    // 5. Выполнение запроса с защитой
                    var result = query?
                        .OrderByDescending(c => c.ContractDate)
                        .AsNoTracking()
                        .ToList() ?? new List<ClientContracts>();

                    Debug.WriteLine($"Найдено {result.Count} записей");

                    // 6. Назначение источника данных с проверкой
                    if (ContractsGrid != null)
                    {
                        ContractsGrid.ItemsSource = result;
                    }
                    else
                    {
                        Debug.WriteLine("Ошибка: ContractsGrid is null");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА: {ex}\n{ex.StackTrace}");
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchBox.Text = string.Empty;
                StatusFilter.SelectedIndex = 0;
                LoadContracts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем и показываем главную страницу юриста
                var lawyerPage = new LawyerBasePage();

                // Если LawyerBasePage - это Page, а не Window
                if (Application.Current.MainWindow is NavigationWindow navWindow)
                {
                    navWindow.Navigate(lawyerPage);
                }
                

                // Закрываем текущее окно
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переходе: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем выбранный договор
                var button = sender as Button;
                var contract = button?.DataContext as ClientContracts;

                if (contract == null)
                {
                    MessageBox.Show("Не удалось получить информацию о договоре", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Подтверждение удаления
                var result = MessageBox.Show($"Вы уверены, что хотите удалить договор '{contract.ContractTemplates?.TemplateName}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new Entities())
                    {
                        // Получаем полную версию договора из базы
                        var contractToDelete = context.ClientContracts
                            .Include(c => c.ContractHistory)
                            .FirstOrDefault(c => c.ContractId == contract.ContractId);

                        if (contractToDelete != null)
                        {
                            // Удаляем связанную историю
                            if (contractToDelete.ContractHistory != null)
                            {
                                context.ContractHistory.RemoveRange(contractToDelete.ContractHistory);
                            }

                            // Удаляем сам договор
                            context.ClientContracts.Remove(contractToDelete);
                            context.SaveChanges();

                            // Обновляем список
                            LoadContracts();

                            MessageBox.Show("Договор успешно удален", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении договора: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}