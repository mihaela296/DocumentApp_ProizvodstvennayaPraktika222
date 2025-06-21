using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net.Mail;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : Page
    {
        public RegistrationPage()
        {
            InitializeComponent();
            CmbRoles.ItemsSource = new Entities().Roles.ToList();
        }

        private Users _user = new Users();
        private static bool isValidMail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }

            try
            {
                MailAddress address = new MailAddress(email);
                return address.Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void Registrate()
        {
            StringBuilder errors = new StringBuilder();
            var regex = new Regex(@"^((\+7))\d{10}$");

            if (string.IsNullOrEmpty(TbFullname.Text)) errors.AppendLine("Введите ФИО");
            if (string.IsNullOrEmpty(TbEmail.Text)) errors.AppendLine("Введите Email");
            if (string.IsNullOrEmpty(TbPhoneNumber.Text)) errors.AppendLine("Введите номер телефона");
            if (CmbRoles.SelectedItem == null) errors.AppendLine("Выберите роль");
            if (string.IsNullOrEmpty(TbUsername.Text)) errors.AppendLine("Введите логин");
            if (string.IsNullOrEmpty(PbPassword.Password)) errors.AppendLine("Введите пароль");
            if (string.IsNullOrEmpty(PbPasswordCheck.Password)) errors.AppendLine("Повторите пароль");
            if (!regex.IsMatch(TbPhoneNumber.Text)) errors.AppendLine("Укажите номер телефона в формате +7хххххххххх");
            if (!isValidMail(TbEmail.Text)) errors.AppendLine("Введите корректный Email");

            if (PbPassword.Password.Length > 0)
            {
                bool en = true;
                bool number = false;
                for (int i = 0; i < PbPassword.Password.Length; i++)
                {
                    if (PbPassword.Password[i] >= 'А' && PbPassword.Password[i] <= 'Я') en = false;
                    if (PbPassword.Password[i] >= '0' && PbPassword.Password[i] <= '9') number = true;
                }

                if (PbPassword.Password.Length < 6) errors.AppendLine("Пароль должен быть больше 6 символов");
                if (!en) errors.AppendLine("Пароль должен быть на английском языке");
                if (!number) errors.AppendLine("В пароле должна быть минимум 1 цифра");
            }

            if (PbPassword.Password != PbPasswordCheck.Password) errors.AppendLine("Пароли не совпадают");

            if (TbUsername.Text.Length > 0)
            {
                using (var db = new Entities())
                {
                    var employee = db.Users.AsNoTracking().FirstOrDefault(u => u.Username == TbUsername.Text);
                    if (employee != null) errors.AppendLine("Пользователь с таким логином уже существует");
                }
            }

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString());
                return;
            }

            try
            {
                var context = new Entities();

                _user.Username = TbUsername.Text;
                _user.FullName = TbFullname.Text;
                _user.Phone = TbPhoneNumber.Text;
                _user.Email = TbEmail.Text;
                _user.RoleId = (int)CmbRoles.SelectedValue;
                _user.PasswordHash = PasswordHasher.CreateHash(PbPassword.Password, out string salt);
                _user.PasswordSalt = salt;
                _user.CreatedAt = DateTime.Now;
                _user.IsActive = true;


                context.Users.Add(_user);
                context.SaveChanges();
                MessageBox.Show("Вы зарегистрировались", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                if (_user.RoleId == 1)
                {
                    NavigationService.Navigate(new ClientBasePage(_user));
                }
                else if (_user.RoleId == 2)
                {
                    NavigationService.Navigate(new LawyerBasePage());
                }
                else
                {
                    MessageBox.Show("Ошибка идентификации роли пользователя");
                    return;
                }
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                .SelectMany(x => x.ValidationErrors)
                .Select(x => x.ErrorMessage);

                string fullErrorMessage = string.Join("\n", errorMessages);
                MessageBox.Show("Ошибки валидации:\n" + fullErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            Registrate();
        }
    }
}
