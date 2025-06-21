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

namespace DocumentApp_ProizvodstvennayaPraktika.Pages
{
    /// <summary>
    /// Логика взаимодействия для AuthorizationPage.xaml
    /// </summary>
    public partial class AuthorizationPage : Page
    {
        public AuthorizationPage()
        {
            InitializeComponent();
        }

        private void Authorize()
        {
            if (string.IsNullOrWhiteSpace(TbUsername.Text) || string.IsNullOrEmpty(PbPassword.Password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            var user = new Entities().Users.AsNoTracking()
                        .FirstOrDefault(u => u.Username == TbUsername.Text);

            if (user == null)
            {
                MessageBox.Show("Пользователь с такими данными не найден");
                return;
            }

            bool isValid = PasswordHasher.VerifyPassword(PbPassword.Password,
                          user.PasswordHash, user.PasswordSalt);

            if (!isValid)
            {
                MessageBox.Show("Неверный логин или пароль");
                return;
            }

            // Очищаем поля после успешного входа
            TbUsername.Clear();
            PbPassword.Clear();

            // Правильная проверка ролей
            switch (user.RoleId)
            {
                case 1: // Клиент
                    MessageBox.Show("Добро пожаловать, клиент!");
                    NavigationService?.Navigate(new ClientBasePage(user));
                    break;

                case 2: // Юрист
                    MessageBox.Show("Добро пожаловать, юрист!");
                    NavigationService?.Navigate(new LawyerBasePage());
                    break;

                default:
                    MessageBox.Show("Ошибка: неизвестная роль пользователя");
                    break;
            }
        }
        

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            Authorize();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegistrationPage());
        }
    }
}
