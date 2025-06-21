using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Word = Microsoft.Office.Interop.Word;
using System.Runtime.InteropServices; // Для Marshal.ReleaseComObject
using Newtonsoft.Json; // Для работы с JSON-данными договора

namespace DocumentApp_ProizvodstvennayaPraktika
{
    public partial class ContractViewWindow : Window
    {
        private ClientContracts _contract;
        private Users _currentUser;
        private Dictionary<string, Control> _fieldControls = new Dictionary<string, Control>();
        private bool _isEditMode = false;

        public ContractViewWindow(ClientContracts contract, Users user)
        {
            InitializeComponent();
            _contract = contract;
            _currentUser = user;
            DataContext = _contract;

            LoadContractFields();
            UpdateUI();
        }

        private void LoadContractFields()
        {
            try
            {
                using (var context = new Entities())
                {
                    _contract = context.ClientContracts
                        .Include(c => c.ContractTemplates)
                        .Include(c => c.ContractTemplates.TemplateFields)
                        .FirstOrDefault(c => c.ContractId == _contract.ContractId);

                    if (_contract == null || _contract.ContractTemplates == null)
                    {
                        MessageBox.Show("Договор или шаблон не найден");
                        Close();
                        return;
                    }

                    FieldsContainer.Items.Clear();
                    _fieldControls.Clear();

                    var contractData = string.IsNullOrEmpty(_contract.ContractData)
                        ? new Dictionary<string, string>()
                        : JsonConvert.DeserializeObject<Dictionary<string, string>>(_contract.ContractData);

                    foreach (var field in _contract.ContractTemplates.TemplateFields)
                    {
                        var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
                        var label = new Label { Content = field.FieldLabel };

                        Control inputControl;
                        if (field.FieldType.ToLower() == "date")
                        {
                            var datePicker = new DatePicker();
                            if (contractData.ContainsKey(field.FieldName))
                            {
                                DateTime date;
                                if (DateTime.TryParse(contractData[field.FieldName], out date))
                                {
                                    datePicker.SelectedDate = date;
                                }
                            }
                            inputControl = datePicker;
                        }
                        else
                        {
                            var textBox = new TextBox();
                            if (contractData.ContainsKey(field.FieldName))
                            {
                                textBox.Text = contractData[field.FieldName];
                            }
                            inputControl = textBox;
                        }

                        inputControl.IsEnabled = false;
                        stackPanel.Children.Add(label);
                        stackPanel.Children.Add(inputControl);
                        FieldsContainer.Items.Add(stackPanel);
                        _fieldControls.Add(field.FieldName, inputControl);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки полей: {ex.Message}");
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = true;
            foreach (var control in _fieldControls.Values)
            {
                control.IsEnabled = true;
            }

            EditBtn.Visibility = Visibility.Collapsed;
            SaveBtn.Visibility = Visibility.Visible;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var contractData = new Dictionary<string, string>();
                foreach (var field in _fieldControls)
                {
                    string value = "";
                    if (field.Value is TextBox)
                    {
                        value = ((TextBox)field.Value).Text;
                    }
                    else if (field.Value is DatePicker)
                    {
                        var datePicker = (DatePicker)field.Value;
                        value = datePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
                    }
                    contractData.Add(field.Key, value);
                }

                using (var context = new Entities())
                {
                    var dbContract = context.ClientContracts.Find(_contract.ContractId);
                    if (dbContract != null)
                    {
                        dbContract.ContractData = JsonConvert.SerializeObject(contractData);
                        dbContract.UpdatedAt = DateTime.Now;

                        // Не меняем статус на Completed при повторном сохранении
                        if (dbContract.Status == "Draft")
                        {
                            dbContract.Status = "Completed";
                        }

                        context.SaveChanges();
                        _contract = dbContract;
                        DataContext = _contract;
                    }
                }

                _isEditMode = false;
                foreach (var control in _fieldControls.Values)
                {
                    control.IsEnabled = false;
                }

                EditBtn.Visibility = Visibility.Visible;
                SaveBtn.Visibility = Visibility.Collapsed;

                MessageBox.Show("Изменения сохранены", "Сохранение",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения договора: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            EditBtn.Visibility = Visibility.Visible;
            SaveBtn.Visibility = Visibility.Collapsed;
        }

        private void ExportToWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Диалог сохранения файла
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx",
                    FileName = $"Договор_{_contract.ContractNumber}_{DateTime.Now:yyyyMMdd}.docx",
                    DefaultExt = ".docx",
                    Title = "Сохранить договор"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    // Создаем объект Word
                    Word.Application wordApp = new Word.Application();
                    try
                    {
                        Word.Document document = wordApp.Documents.Add();
                        try
                        {
                            // Заголовок
                            Word.Paragraph title = document.Paragraphs.Add();
                            title.Range.Text = _contract.ContractTemplates.TemplateName;
                            title.Range.Font.Bold = 1;
                            title.Range.Font.Size = 16;
                            title.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                            title.Range.InsertParagraphAfter();

                            // Информация о договоре
                            Word.Paragraph info = document.Paragraphs.Add();
                            info.Range.Text = $"Номер: {_contract.ContractNumber}\nДата: {_contract.ContractDate:dd.MM.yyyy}\nСтатус: {_contract.Status}";
                            info.Format.SpaceAfter = 10;
                            info.Range.InsertParagraphAfter();

                            // Поля договора (если есть)
                            if (!string.IsNullOrEmpty(_contract.ContractData))
                            {
                                var fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(_contract.ContractData);
                                foreach (var field in _contract.ContractTemplates.TemplateFields)
                                {
                                    if (fields.ContainsKey(field.FieldName))
                                    {
                                        Word.Paragraph fieldParagraph = document.Paragraphs.Add();
                                        fieldParagraph.Range.Text = $"{field.FieldLabel}: {fields[field.FieldName]}";
                                        fieldParagraph.Range.InsertParagraphAfter();
                                    }
                                }
                            }

                            // Основной текст
                            Word.Paragraph content = document.Paragraphs.Add();
                            content.Range.Text = _contract.ContractTemplates.Content;
                            content.Format.SpaceBefore = 10;

                            // Сохраняем
                            document.SaveAs2(filePath, Word.WdSaveFormat.wdFormatDocumentDefault);
                            MessageBox.Show($"Договор сохранён:\n{filePath}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        finally
                        {
                            document.Close(false);
                            Marshal.ReleaseComObject(document);
                        }
                    }
                    finally
                    {
                        wordApp.Quit();
                        Marshal.ReleaseComObject(wordApp);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditMode)
            {
                var result = MessageBox.Show("У вас есть несохраненные изменения. Закрыть без сохранения?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            Close();
        }
    }
}