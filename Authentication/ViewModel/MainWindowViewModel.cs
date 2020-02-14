using System.Windows;
using System.Windows.Input;
using Authentication.View;
using Database;

namespace Authentication.ViewModel {
    public class MainWindowViewModel : BaseViewModel {
        private readonly DatabaseInteraction _database;
        private readonly AuthenticationWindow _regWindow;

        private string _tbLoginText;

        public string TbLoginText {
            get => _tbLoginText;
            set {
                _tbLoginText = value;
                OnPropertyChanged(nameof(TbLoginText));
            }
        }

        public MainWindowViewModel() {
            try {
                _database = new DatabaseInteraction();
            } catch (Npgsql.PostgresException e) {
                MessageBox.Show(e.Message);
                throw;
            }

            _regWindow = (AuthenticationWindow)Application.Current.MainWindow;
            SignInCommand = new DelegateCommand(SignIn, CanSignIn);
        }

        public ICommand SignInCommand { get; }
        
        private void SignIn(object sender) {
            try {
                MessageBox.Show(_database.SignIn(TbLoginText, _regWindow.TbPassword.Password)
                                    ? "Вы успешно аутентифицированы!"
                                    : "Логин или пароль неверны!", "Уведомление");
            } catch {
                MessageBox.Show("Ошибка! Нет доступа к базе данных1.");
            }
        }
        
        private bool CanSignIn(object sender) {
            return !string.IsNullOrEmpty(TbLoginText) && !string.IsNullOrEmpty(_regWindow.TbPassword.Password);
        }
    }
}