using DocumentApp_ProizvodstvennayaPraktika.Pages;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DocumentApp_ProizvodstvennayaPraktika
{
    public partial class TemplateManagementWindow : Window
    {
        private List<ContractTemplates> _allTemplates;
        private List<ContractCategories> _categories;

        public TemplateManagementWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new Entities())
                {
                    _allTemplates = context.ContractTemplates
                        .Include(t => t.ContractCategories)
                        .Include(t => t.TemplateFields)
                        .OrderBy(t => t.TemplateId)
                        .ToList();

                    TemplatesGrid.ItemsSource = _allTemplates;

                    _categories = context.ContractCategories.ToList();
                    CategoryFilter.ItemsSource = _categories;
                    CategoryFilter.SelectedIndex = -1;
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
                TemplatesGrid.ItemsSource = _allTemplates;
                return;
            }

            var searchText = SearchBox.Text.ToLower();
            var filtered = _allTemplates
                .Where(t => t.TemplateName.ToLower().Contains(searchText))
                .ToList();

            TemplatesGrid.ItemsSource = filtered;
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryFilter.SelectedItem is ContractCategories selectedCategory)
            {
                var filtered = _allTemplates
                    .Where(t => t.CategoryId == selectedCategory.CategoryId)
                    .ToList();
                TemplatesGrid.ItemsSource = filtered;
            }
            else
            {
                TemplatesGrid.ItemsSource = _allTemplates;
            }
        }

        private void AddTemplate_Click(object sender, RoutedEventArgs e)
        {
            var newTemplate = new ContractTemplates
            {
                TemplateName = "Новый шаблон",
                Content = " ",
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = GetDefaultUserId() // Используем CreatedBy вместо UserId
            };

            var editor = new TemplateEditorWindow(newTemplate);

            if (editor.ShowDialog() == true && editor.Template != null)
            {
                if (SafeSaveTemplate(editor.Template))
                {
                    LoadData();
                }
            }
        }

        private int GetDefaultUserId()
        {
            try
            {
                using (var context = new Entities())
                {
                    var user = context.Users.FirstOrDefault();
                    return user?.UserId ?? 1;
                }
            }
            catch
            {
                return 1;
            }
        }

        private bool SafeSaveTemplate(ContractTemplates template)
        {
            try
            {
                using (var context = new Entities())
                {
                    if (string.IsNullOrWhiteSpace(template.TemplateName))
                    {
                        MessageBox.Show("Название шаблона не может быть пустым", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    // Убедимся, что CreatedBy установлен
                    if (template.CreatedBy == 0)
                    {
                        template.CreatedBy = GetDefaultUserId();
                    }

                    // ... остальная логика сохранения ...

                    context.ContractTemplates.Add(template);
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении шаблона: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private string GetExceptionDetails(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message);

            Exception inner = ex.InnerException;
            while (inner != null)
            {
                sb.AppendLine($"-> {inner.Message}");
                inner = inner.InnerException;
            }

            return sb.ToString();
        }

        private void EditTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesGrid.SelectedItem is ContractTemplates template)
            {
                var editor = new TemplateEditorWindow(template);
                if (editor.ShowDialog() == true)
                {
                    try
                    {
                        using (var context = new Entities())
                        {
                            var dbTemplate = context.ContractTemplates.Find(template.TemplateId);
                            if (dbTemplate != null)
                            {
                                dbTemplate.TemplateName = editor.Template.TemplateName;
                                dbTemplate.Content = editor.Template.Content;
                                dbTemplate.CategoryId = editor.Template.CategoryId;
                                dbTemplate.IsActive = editor.Template.IsActive;
                                dbTemplate.UpdatedAt = DateTime.Now;

                                context.SaveChanges();
                                LoadData();
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

        private void EditFields_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesGrid.SelectedItem is ContractTemplates template)
            {
                var fieldsWindow = new TemplateFieldsWindow(template);
                fieldsWindow.ShowDialog();
                LoadData();
            }
        }

        private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesGrid.SelectedItem is ContractTemplates template)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить шаблон '{template.TemplateName}'?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new Entities())
                        {
                            // Проверяем, используется ли шаблон в договорах
                            bool isUsed = context.ClientContracts.Any(c => c.TemplateId == template.TemplateId);

                            if (isUsed)
                            {
                                MessageBox.Show("Нельзя удалить шаблон, который используется в договорах. " +
                                    "Сначала удалите все связанные договоры.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // Удаляем связанные поля шаблона
                            var fields = context.TemplateFields.Where(f => f.TemplateId == template.TemplateId).ToList();
                            context.TemplateFields.RemoveRange(fields);

                            // Удаляем сам шаблон
                            var dbTemplate = context.ContractTemplates.Find(template.TemplateId);
                            if (dbTemplate != null)
                            {
                                context.ContractTemplates.Remove(dbTemplate);
                                context.SaveChanges();
                                LoadData();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении шаблона: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            CategoryFilter.SelectedIndex = -1;
            LoadData();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}