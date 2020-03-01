using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Registration.View;
using Database;

namespace Registration.ViewModel {
    public class RegistrationWindowViewModel : BaseViewModel {
        private readonly DatabaseInteraction _database;
        private readonly RegistrationWindow _parentWindow;

        private string _tbLoginText;
        private string _imgPasswordStrengthPath;
        private Visibility _imgPasswordVisibility;

        private readonly Dictionary<int, string> _imgStrengthPaths = new Dictionary<int, string> {
            {0, "../Images/PasswordStrength0.png"},
            {1, "../Images/PasswordStrength1.png"},
            {2, "../Images/PasswordStrength2.png"},
            {3, "../Images/PasswordStrength3.png"},
            {4, "../Images/PasswordStrength4.png"}
        };

        public string TbLoginText {
            get => _tbLoginText;
            set {
                _tbLoginText = value;
                _database.CheckLogin(_tbLoginText);
                OnPropertyChanged(nameof(TbLoginText));
            }
        }

        public string ImgPasswordStrengthPath {
            get => _imgPasswordStrengthPath;
            set {
                _imgPasswordStrengthPath = value;
                OnPropertyChanged(nameof(ImgPasswordStrengthPath));
            }
        }

        public Visibility ImgPasswordVisibility {
            get => _imgPasswordVisibility;
            set {
                _imgPasswordVisibility = value;
                OnPropertyChanged(nameof(ImgPasswordVisibility));
            }
        }

        public RegistrationWindowViewModel() {
            try {
                _database = new DatabaseInteraction();
            } catch (Npgsql.PostgresException e) {
                MessageBox.Show(e.Message);
                throw;
            }
            
            _parentWindow = RegistrationWindow.Instance;

            RegisterUserCommand = new DelegateCommand(RegisterUser, CanRegisterUser);
            _database.PasswordStrengthChanged += (type, o) => {
                if (type == EventStringTypes.PasswordStrengthIndex) {
                    ImgPasswordStrengthPath = _imgStrengthPaths[(int) o];
                }
            };

            _database.ErrorInfoChanged += (type, o) => {
                switch (type) {
                    case EventStringTypes.Login:
                        ((ErrorProviderViewModel) _parentWindow.TbLogin.DataContext).ErrorName = o as string;
                        break;
                    case EventStringTypes.Password:
                        ((ErrorProviderViewModel) _parentWindow.TbPassword.DataContext).ErrorName = o as string;
                        break;
                }
                CommandManager.InvalidateRequerySuggested();
            };
        }

        public void TbPassword_OnLostFocus(object sender, RoutedEventArgs e) {
            ImgPasswordVisibility = Visibility.Hidden;
        }

        public void TbPassword_OnPasswordChanged(object sender, RoutedEventArgs e) {
            _database.CheckPassword(((PasswordBox) sender).Password);

            if (ImgPasswordVisibility != Visibility.Visible) {
                ImgPasswordVisibility = Visibility.Visible;
            }
        }

        public ICommand RegisterUserCommand { get; }

        private void RegisterUser(object sender) {
            try {
                MessageBox.Show(
                    _database.RegisterUser(TbLoginText, _parentWindow.TbPassword.Password)
                        ? "Вы успешно зарегистрированы!"
                        : "Ошибка, некорректные данные. Запрос на регистрацию отклонён!", "Уведомление");
                TbLoginText = TbLoginText;
            } catch (Npgsql.PostgresException e) {
                MessageBox.Show(e.Message);
            }
        }

        private bool CanRegisterUser(object sender) {
            var tbLoginEp = (ErrorProviderViewModel) _parentWindow.TbLogin.DataContext;
            var tbPasswordEp = (ErrorProviderViewModel) _parentWindow.TbPassword.DataContext;
            return string.IsNullOrEmpty(tbLoginEp.ErrorName) && string.IsNullOrEmpty(tbPasswordEp.ErrorName) &&
                   !string.IsNullOrEmpty(TbLoginText) && !string.IsNullOrEmpty(_parentWindow.TbPassword.Password);
        }
    }
}