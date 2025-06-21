using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DocumentApp_ProizvodstvennayaPraktika.Pages
{
    public partial class ClientBasePage : Page
    {
        private Users _currentUser;
        private List<ClientContracts> _allContracts;
        private List<ContractCategories> _categories;
        private List<ContractTemplates> _availableTemplates;

        public ClientBasePage(Users user)
        {
            InitializeComponent();
            _currentUser = user;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    _allContracts = context.ClientContracts
                        .Include(c => c.ContractTemplates) // Явно включаем загрузку шаблонов
                        .Include(c => c.ContractTemplates.ContractCategories)
                        .Where(c => c.ClientId == _currentUser.UserId)
                        .Where(c => c.ContractTemplates != null)
                        .ToList();

                    // Убедимся, что названия загружены
                    foreach (var contract in _allContracts)
                    {
                        if (contract.ContractTemplates != null && string.IsNullOrEmpty(contract.ContractTemplates.TemplateName))
                        {
                            contract.ContractTemplates.TemplateName = "Без названия";
                        }
                    }

                    ContractsGrid.ItemsSource = _allContracts ?? new List<ClientContracts>();

                    _categories = context.ContractCategories
                        .Where(c => c != null)
                        .ToList();
                    FilterComboBox.ItemsSource = _categories;

                    // Исправлено условие для nullable bool
                    _availableTemplates = context.ContractTemplates
                        .Include(t => t.ContractCategories)
                        .Where(t => t != null && (t.IsActive ?? false))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                ContractsGrid.ItemsSource = _allContracts;
                return;
            }

            var searchText = SearchBox.Text.ToLower();
            var filtered = _allContracts?
                .Where(c =>
                    (c.ContractTemplates?.TemplateName?.ToLower()?.Contains(searchText) ?? false) ||
                    c.ContractId.ToString().Contains(searchText))
                .ToList() ?? new List<ClientContracts>();

            ContractsGrid.ItemsSource = filtered;
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterComboBox.SelectedItem is ContractCategories selectedCategory)
            {
                var filtered = _allContracts?
                    .Where(c =>
                        c.ContractTemplates != null &&
                        c.ContractTemplates.CategoryId == selectedCategory.CategoryId)
                    .ToList() ?? new List<ClientContracts>();

                ContractsGrid.ItemsSource = filtered;
            }
            else
            {
                ContractsGrid.ItemsSource = _allContracts;
            }
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            var sorted = _allContracts?
                .OrderBy(c => c.ContractId)
                .ToList() ?? new List<ClientContracts>();
            ContractsGrid.ItemsSource = sorted;
        }

        private void CreateContract_Click(object sender, RoutedEventArgs e)
        {
            var templateDialog = new SelectTemplateDialog(_availableTemplates);
            if (templateDialog.ShowDialog() == true && templateDialog.SelectedTemplate != null)
            {
                try
                {
                    using (var context = new Entities())
                    {
                        // Создаем договор с заполнением всех обязательных полей
                        var newContract = new ClientContracts
                        {
                            ClientId = _currentUser.UserId,
                            TemplateId = templateDialog.SelectedTemplate.TemplateId,
                            ContractDate = DateTime.Now,
                            Status = "Draft",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = null,
                            ContractNumber = $"Д-{DateTime.Now:yyyyMMdd-HHmmss}",
                            ContractData = "{}"
                        };

                        // Проверяем валидность данных перед сохранением
                        var errors = new List<string>();

                        if (newContract.ClientId <= 0)
                            errors.Add("Не указан ID клиента");

                        if (newContract.TemplateId <= 0)
                            errors.Add("Не указан ID шаблона");

                        if (string.IsNullOrEmpty(newContract.ContractNumber))
                            errors.Add("Не указан номер договора");

                        if (string.IsNullOrEmpty(newContract.Status))
                            errors.Add("Не указан статус договора");

                        if (newContract.ContractDate == default)
                            errors.Add("Не указана дата договора");

                        if (newContract.CreatedAt == default)
                            errors.Add("Не указана дата создания");

                        if (string.IsNullOrEmpty(newContract.ContractData))
                            errors.Add("Не указаны данные договора");

                        if (errors.Any())
                        {
                            MessageBox.Show($"Ошибки валидации:\n{string.Join("\n", errors)}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        context.ClientContracts.Add(newContract);
                        context.SaveChanges();

                        // Обновляем данные и открываем договор
                        LoadData();

                        var viewWindow = new ContractViewWindow(
                            context.ClientContracts
                                .Include(c => c.ContractTemplates)
                                .First(c => c.ContractId == newContract.ContractId),
                            _currentUser);

                        viewWindow.ShowDialog();
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => $"{x.PropertyName}: {x.ErrorMessage}");

                    MessageBox.Show($"Ошибки валидации сущности:\n{string.Join("\n", errorMessages)}",
                        "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании договора: {ex.Message}\n\n" +
                                  $"Inner exception: {ex.InnerException?.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Генерация номера договора
        private string GenerateContractNumber()
        {
            return $"Д-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
        }

        private void FillContract_Click(object sender, RoutedEventArgs e)
        {
            if (ContractsGrid.SelectedItem is ClientContracts selectedContract)
            {
                try
                {
                    using (var context = new Entities())
                    {
                        var fullContract = context.ClientContracts
                            .Include(c => c.ContractTemplates)
                            .Include(c => c.ContractTemplates.TemplateFields)
                            .FirstOrDefault(c => c.ContractId == selectedContract.ContractId);

                        if (fullContract != null)
                        {
                            var viewWindow = new ContractViewWindow(fullContract, _currentUser);
                            viewWindow.ShowDialog();
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия договора: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите договор для заполнения", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ContractsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FillContract_Click(sender, e);
        }
    }

    public class SelectTemplateDialog : Window
    {
        public ContractTemplates SelectedTemplate { get; private set; }

        public SelectTemplateDialog(List<ContractTemplates> templates)
        {
            InitializeComponent(templates);
        }

        private void InitializeComponent(List<ContractTemplates> templates)
        {
            Title = "Выбор шаблона договора";
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                ItemsSource = templates
            };

            dataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Название",
                Binding = new Binding("TemplateName"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            dataGrid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Категория",
                Binding = new Binding("ContractCategories.CategoryName"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            dataGrid.MouseDoubleClick += (s, e) =>
            {
                if (dataGrid.SelectedItem is ContractTemplates template)
                {
                    SelectedTemplate = template;
                    DialogResult = true;
                    Close();
                }
            };

            Grid.SetRow(dataGrid, 0);
            grid.Children.Add(dataGrid);

            var buttonPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var selectButton = new Button()
            {
                Content = "Выбрать",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5)
            };

            selectButton.Click += (s, e) =>
            {
                if (dataGrid.SelectedItem is ContractTemplates template)
                {
                    SelectedTemplate = template;
                    DialogResult = true;
                    Close();
                }
            };

            var cancelButton = new Button()
            {
                Content = "Отмена",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5)
            };

            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(selectButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}