using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace DocumentApp_ProizvodstvennayaPraktika
{
    public partial class TemplateEditorWindow : Window, INotifyPropertyChanged
    {
        private string _templateName;
        public string TemplateName
        {
            get => _templateName;
            set
            {
                _templateName = value;
                OnPropertyChanged();
            }
        }

        public ContractTemplates Template { get; private set; }
        public List<ContractCategories> Categories { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public TemplateEditorWindow(ContractTemplates template)
        {
            InitializeComponent();

            Template = template ?? new ContractTemplates
            {
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = ((App)Application.Current).CurrentUserId // Или другой способ получения ID
            };

            TemplateName = Template.TemplateName ?? string.Empty;
            TemplateNameTextBox.Focus();

            LoadCategories();
            DataContext = this;
        }
        public partial class App : Application
        {
            public int CurrentUserId { get; set; }

            protected override void OnStartup(StartupEventArgs e)
            {
                // Инициализация CurrentUserId после аутентификации
                base.OnStartup(e);
            }
        }
        private int GetDefaultUserId()
        {
            try
            {
                using (var context = new Entities())
                {
                    // Получаем первого пользователя из базы
                    var user = context.Users.FirstOrDefault();
                    return user?.UserId ?? 1; // Возвращаем 1, если пользователей нет
                }
            }
            catch
            {
                return 1; // Запасное значение
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadCategories()
        {
            using (var context = new Entities())
            {
                Categories = context.ContractCategories.ToList();
                OnPropertyChanged(nameof(Categories));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем введенное название
            if (string.IsNullOrWhiteSpace(TemplateName))
            {
                MessageBox.Show("Введите название шаблона", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TemplateNameTextBox.Focus();
                return;
            }

            // Убедимся, что все обязательные поля заполнены
            Template.TemplateName = TemplateName.Trim();
            Template.UpdatedAt = DateTime.Now;

            // Проверяем категорию
            if (Template.CategoryId == 0 && Categories != null && Categories.Any())
            {
                Template.CategoryId = Categories.First().CategoryId;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TemplateNameTextBox.SelectAll();
        }
    }
}