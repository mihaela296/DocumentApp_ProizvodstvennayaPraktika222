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

namespace DocumentApp_ProizvodstvennayaPraktika
{
    /// <summary>
    /// Логика взаимодействия для ClientEditorWindow.xaml
    /// </summary>
    public partial class ClientEditorWindow : Window
    {
        public Users Client { get; set; }
        public string Password { get; set; }

        public ClientEditorWindow(Users client)
        {
            InitializeComponent();
            Client = client;
            DataContext = Client;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Client.FullName) ||
                string.IsNullOrWhiteSpace(Client.Email) ||
                string.IsNullOrWhiteSpace(Client.Username))
            {
                MessageBox.Show("Заполните все обязательные поля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сохраняем пароль из PasswordBox
            Password = PasswordBox.Password;

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
