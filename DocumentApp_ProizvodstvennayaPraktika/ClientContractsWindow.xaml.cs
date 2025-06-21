using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace DocumentApp_ProizvodstvennayaPraktika
{
    public partial class ClientContractsWindow : Window
    {
        private int _clientId;
        private string _clientName;


        public string ClientName => $"Договоры клиента: {_clientName}";

        public ClientContractsWindow(int clientId)
        {
            InitializeComponent();
            _clientId = clientId;
            LoadClientData();
            DataContext = this;
        }

        private void LoadClientData()
        {
            try
            {
                using (var context = new Entities())
                {
                    ContractsGrid.ItemsSource = context.ClientContracts
                        .Include(c => c.ContractTemplates)
                        .Where(c => c.ClientId == _clientId)
                        .Where(c => c.ContractTemplates != null) // Исключаем договоры без шаблона
                        .Where(c => !string.IsNullOrEmpty(c.ContractTemplates.TemplateName)) // Исключаем пустые названия
                        .OrderBy(c => c.ContractId) // Сортируем по ID
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки договоров: {ex.Message}");
            }
        }

        private void ViewContract_Click(object sender, RoutedEventArgs e)
        {
            if (ContractsGrid.SelectedItem is ClientContracts contract)
            {
                var client = new Users { UserId = _clientId, FullName = _clientName };
                var viewWindow = new ContractViewWindow(contract, client);
                viewWindow.ShowDialog();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}