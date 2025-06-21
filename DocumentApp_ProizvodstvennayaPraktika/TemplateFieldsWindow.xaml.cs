using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DocumentApp_ProizvodstvennayaPraktika
{
    public partial class TemplateFieldsWindow : Window
    {
        private ContractTemplates _template;
        private List<TemplateFields> _fields;
        public List<string> FieldTypes { get; } = new List<string> { "text", "date", "number" };

        public string TemplateName => $"Поля шаблона: {_template.TemplateName}";

        public TemplateFieldsWindow(ContractTemplates template)
        {
            InitializeComponent();
            _template = template;
            LoadFields();
            DataContext = this;
        }

        private void LoadFields()
        {
            using (var context = new Entities())
            {
                _fields = context.TemplateFields
                    .Where(f => f.TemplateId == _template.TemplateId)
                    .ToList();
                FieldsGrid.ItemsSource = _fields;
            }
        }

        private void AddField_Click(object sender, RoutedEventArgs e)
        {
            _fields.Add(new TemplateFields
            {
                TemplateId = _template.TemplateId,
                FieldType = "text",
                IsRequired = true
            });
            FieldsGrid.Items.Refresh();
        }

        private void RemoveField_Click(object sender, RoutedEventArgs e)
        {
            if (FieldsGrid.SelectedItem is TemplateFields field)
            {
                _fields.Remove(field);
                FieldsGrid.Items.Refresh();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new Entities())
                {
                    // Удаляем старые поля
                    var oldFields = context.TemplateFields
                        .Where(f => f.TemplateId == _template.TemplateId)
                        .ToList();
                    context.TemplateFields.RemoveRange(oldFields);

                    // Добавляем новые поля
                    foreach (var field in _fields)
                    {
                        if (string.IsNullOrWhiteSpace(field.FieldLabel) ||
                            string.IsNullOrWhiteSpace(field.FieldName))
                        {
                            MessageBox.Show("Заполните названия всех полей", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        context.TemplateFields.Add(field);
                    }

                    context.SaveChanges();
                    DialogResult = true;
                    Close();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения полей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}