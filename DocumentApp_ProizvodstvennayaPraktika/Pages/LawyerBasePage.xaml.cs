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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DocumentApp_ProizvodstvennayaPraktika.Pages; // Или где у вас находится LawyerBasePage

namespace DocumentApp_ProizvodstvennayaPraktika.Pages
{
    /// <summary>
    /// Логика взаимодействия для LaywerBasePage.xaml
    /// </summary>
    public partial class LawyerBasePage : Page
    {
        public LawyerBasePage()
        {
            InitializeComponent();
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            using (var context = new Entities())
            {
                // Активные договоры
                ActiveContractsCount.Text = context.ClientContracts
                    .Count(c => c.Status == "Active").ToString();

                // Клиенты - используем ТОЧНО ТЕ ЖЕ условия, что и в ClientManagementWindow
                ClientsCount.Text = context.Users
                    .Where(u => u.RoleId == 1) // Только клиенты
                    .Where(u => !string.IsNullOrEmpty(u.FullName) &&
                           !string.IsNullOrEmpty(u.Email) &&
                           u.CreatedAt != null) // Только валидные записи
                    .Count()
                    .ToString();

                // Шаблоны
                TemplatesCount.Text = context.ContractTemplates.Count().ToString();
            }
        }

        private void ManageTemplates_Click(object sender, RoutedEventArgs e)
        {
            var window = new TemplateManagementWindow();
            window.ShowDialog();
            LoadStatistics();
        }

        private void ManageClients_Click(object sender, RoutedEventArgs e)
        {
            var window = new ClientManagementWindow();
            window.ShowDialog();
            LoadStatistics();
        }

        private void ViewAllContracts_Click(object sender, RoutedEventArgs e)
        {
            var window = new ContractManagementWindow();
            window.ShowDialog();
        }
    }
}
