using System.Windows.Media;

namespace Registration.ViewModel {
    public class ErrorProviderViewModel : BaseViewModel {
        private string _errorName;
        public string ErrorName {
            get => _errorName;
            set {
                _errorName = value;
                if (value == "") {
                    ToolTipNameEnable = false;
                    BorderTbName = Brushes.Gray;
                } else {
                    ToolTipNameEnable = true;
                    BorderTbName = Brushes.Red;
                }
                OnPropertyChanged(nameof(ErrorName));
            }
        }

        private Brush _borderTbName;
        public Brush BorderTbName {
            get => _borderTbName;
            set {
                _borderTbName = value;
                OnPropertyChanged(nameof(BorderTbName));
            }
        }

        private bool _toolTipNameEnable;
        public bool ToolTipNameEnable {
            get => _toolTipNameEnable;
            set {
                _toolTipNameEnable = value;
                OnPropertyChanged(nameof(ToolTipNameEnable));
            }
        }
    }
}