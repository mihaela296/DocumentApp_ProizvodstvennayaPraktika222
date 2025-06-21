using DocumentFormat.OpenXml.ExtendedProperties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Word = Microsoft.Office.Interop.Word;
using System.Runtime.InteropServices;

namespace DocumentApp_ProizvodstvennayaPraktika
{
    /// <summary>
    /// Логика взаимодействия для ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow : Window
    {
        private ClientContracts _contract;

        public ExportWindow(ClientContracts contract)
        {
            InitializeComponent();
            _contract = contract;
        }
        private void ExportToWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем диалог сохранения файла
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx",
                    FileName = $"Договор_{_contract.ContractNumber}_{DateTime.Now:yyyyMMdd}.docx",
                    DefaultExt = ".docx",
                    Title = "Сохранить договор как"
                };

                // Показываем диалог и проверяем результат
                if (saveFileDialog.ShowDialog() == true)
                {
                    // Получаем путь для сохранения
                    string filePath = saveFileDialog.FileName;

                    // Создаем приложение Word
                    var wordApp = new Word.Application();
                    try
                    {
                        // Создаем новый документ
                        var document = wordApp.Documents.Add();
                        try
                        {
                            // Добавляем заголовок договора
                            var paragraph = document.Paragraphs.Add();
                            paragraph.Range.Text = _contract.ContractTemplates.TemplateName;
                            paragraph.Range.Font.Bold = 1;
                            paragraph.Range.Font.Size = 16;
                            paragraph.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                            paragraph.Range.InsertParagraphAfter();

                            // Добавляем информацию о договоре
                            var infoParagraph = document.Paragraphs.Add();
                            infoParagraph.Range.Text = $"Номер договора: {_contract.ContractNumber}\n" +
                                                      $"Дата: {_contract.ContractDate:dd.MM.yyyy}\n" +
                                                      $"Статус: {_contract.Status}";
                            infoParagraph.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                            infoParagraph.Range.InsertParagraphAfter();

                            // Добавляем разделитель
                            var separator = document.Paragraphs.Add();
                            separator.Range.Text = new string('-', 50);
                            separator.Format.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                            separator.Range.InsertParagraphAfter();

                            // Добавляем заполненные поля договора
                            if (!string.IsNullOrEmpty(_contract.ContractData))
                            {
                                var fieldsData = JsonConvert.DeserializeObject<Dictionary<string, string>>(_contract.ContractData);

                                foreach (var field in _contract.ContractTemplates.TemplateFields)
                                {
                                    if (fieldsData.ContainsKey(field.FieldName))
                                    {
                                        var fieldParagraph = document.Paragraphs.Add();
                                        fieldParagraph.Range.Text = $"{field.FieldLabel}: {fieldsData[field.FieldName]}";
                                        fieldParagraph.Range.InsertParagraphAfter();
                                    }
                                }
                            }

                            // Добавляем основной текст договора
                            var contentParagraph = document.Paragraphs.Add();
                            contentParagraph.Range.Text = _contract.ContractTemplates.Content;
                            contentParagraph.Range.InsertParagraphAfter();

                            // Сохраняем документ
                            document.SaveAs2(filePath);

                            MessageBox.Show($"Договор успешно экспортирован в файл:\n{filePath}",
                                "Экспорт завершен",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        finally
                        {
                            // Закрываем документ
                            document.Close(false);
                        }
                    }
                    finally
                    {
                        // Закрываем Word
                        wordApp.Quit();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Word: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void ExportAndSend_Click(object sender, RoutedEventArgs e)
        {
            // Здесь будет код для экспорта и отправки
            MessageBox.Show("Договор успешно экспортирован и отправлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
