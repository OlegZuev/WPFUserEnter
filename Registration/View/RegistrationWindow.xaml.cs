using System.Windows;
using System.Windows.Input;

namespace Registration.View {
    /// <summary>
    /// Interaction logic for RegistrationWindow.xaml
    /// </summary>
    public partial class RegistrationWindow : Window {
        public static RegistrationWindow Instance;

        public RegistrationWindow() {
            Instance = this;
            InitializeComponent();
            TbPassword.LostFocus += VmMainWindow.TbPassword_OnLostFocus;
            TbPassword.PasswordChanged += VmMainWindow.TbPassword_OnPasswordChanged;

            PreviewKeyDown += (sender, e) => {
                if (e.Key == Key.Escape)
                    Close();
            };
        }
    }
}
