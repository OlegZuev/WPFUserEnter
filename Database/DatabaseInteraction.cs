using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;
using _BCrypt = BCrypt.Net.BCrypt;

namespace Database {
    public class DatabaseInteraction {
        private readonly string _sConnection = new NpgsqlConnectionStringBuilder {
            Host = DatabaseSettings.Default.Host,
            Port = DatabaseSettings.Default.Port,
            Database = DatabaseSettings.Default.Name,
            Username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME"),
            Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD"),
            MaxAutoPrepare = 10,
            AutoPrepareMinUsages = 2
        }.ConnectionString;

        private readonly Regex _regex = new Regex(@"^[а-я0-9a-zё`!@#$%^&*()_\-=+{}\[\];:'"",<.>/?\\*№~|\s]{6,50}$",
                                                  RegexOptions.IgnoreCase);

        private readonly DispatcherTimeout _timerLogin = new DispatcherTimeout {Interval = TimeSpan.FromSeconds(0.75)};

        public DatabaseInteraction() {
            using (var testConn = new NpgsqlConnection(_sConnection)) {
                testConn.Open();
            }
        }

        public void CheckLogin(string login) {
            _timerLogin.ForceStop();
            _timerLogin.Tick += (sender, args) => {
                CheckLoginTimer(login);
                ((DispatcherTimeout) sender).Stop();
            };
            _timerLogin.Start();
        }

        private void CheckLoginTimer(string login) {
            string errorInfo = string.Empty;
            if (LoginExists(login)) {
                errorInfo = "Данный логин уже занят";
            } else if (!IsLoginCorrect(login)) {
                errorInfo =
                    "Логин должен содержать от 6 до 50 символов, в вводе допустимы символы, изображённые " +
                    "на классической русско-английской раскладке клавиатуре, а также любые пробельные символы";
            }

            OnErrorInfoChanged(EventStringTypes.Login, errorInfo);
        }

        private bool LoginExists(string login) {
            using (var conn = new NpgsqlConnection(_sConnection)) {
                conn.Open();
                var command = new NpgsqlCommand {
                    Connection = conn,
                    CommandText = @"SELECT count(*)
                                    FROM users
                                    WHERE lower(login) = lower(@userLogin);"
                };
                command.Parameters.AddWithValue("@userLogin", ClearLogin(login));
                return (long) command.ExecuteScalar() > 0;
            }
        }

        private bool IsLoginCorrect(string login) {
            return _regex.IsMatch(login);
        }

        private static string ClearLogin(string login) {
            login = login.Trim();
            return Regex.Replace(login, @"\s+", " ");
        }

        public void CheckPassword(string password) {
            int result = Zxcvbn.Zxcvbn.MatchPassword(password).Score;
            string errorInfo = result < 2 ? "Слабый пароль" : "";
            OnErrorInfoChanged(EventStringTypes.Password, errorInfo);
            result = result < 1 ? 1 : result;
            if (string.IsNullOrEmpty(password)) {
                result = 0;
            }

            OnPasswordStrengthChanged(EventStringTypes.PasswordStrengthIndex, result);
        }

        private static string EncryptPassword(string password) {
            return _BCrypt.HashPassword(password, 12);
        }

        public bool RegisterUser(string login, string password) {
            login = ClearLogin(login);
            if (login.Length < 6) {
                return false;
            }

            password = password.Trim();
            password = EncryptPassword(password);

            using (var connection = new NpgsqlConnection(_sConnection)) {
                connection.Open();
                var command = new NpgsqlCommand {
                    Connection = connection,
                    CommandText = @"INSERT INTO users (login, password) VALUES (@userLogin, @userPassword) RETURNING id",
                };
                command.Parameters.AddWithValue("@userLogin", login);
                // Соль хранится в начале пароля
                command.Parameters.AddWithValue("@userPassword", password);

                long id = (long) command.ExecuteScalar();
                NewUserAdded?.Invoke(id);
            }

            return true;
        }

        public bool SignIn(string login, string password) {
            login = ClearLogin(login);
            // ReSharper disable once RedundantAssignment
            string passwordHash = string.Empty;

            using (var connection = new NpgsqlConnection(_sConnection)) {
                connection.Open();
                var command = new NpgsqlCommand {
                    Connection = connection,
                    CommandText = @"SELECT password FROM users WHERE login = @userLogin"
                };
                command.Parameters.AddWithValue("@userLogin", login);
                // Соль хранится в начале пароля
                passwordHash = command.ExecuteScalar() as string;
                if (passwordHash == null) {
                    return false;
                }
            }

            return _BCrypt.Verify(password.Trim(), passwordHash);
        }

        public bool ChangeUser(long id, string login, string password, bool passwordChanged, DateTime registrationDate) {
            login = ClearLogin(login);
            if (login.Length < 6) {
                return false;
            }

            if (passwordChanged) {
                password = password.Trim();
                password = EncryptPassword(password);
            }

            using (var connection = new NpgsqlConnection(_sConnection)) {
                connection.Open();
                var command = new NpgsqlCommand {
                    Connection = connection,
                    CommandText = @"UPDATE users SET login = @userLogin, password = @userPassword, registration_date = @userRegistrationDate WHERE id = @userId",
                };
                command.Parameters.AddWithValue("@userLogin", login);
                command.Parameters.AddWithValue("@userPassword", password);
                command.Parameters.AddWithValue("@userRegistrationDate", registrationDate);
                command.Parameters.AddWithValue("@userId", id);

                command.ExecuteNonQuery();
                SomeUserChanged?.Invoke(id);
            }

            return true;
        }

        public delegate void EventString(EventStringTypes objectType, object arg);

        public event EventString ErrorInfoChanged;

        private void OnErrorInfoChanged(EventStringTypes senderType, string errorInfo) {
            ErrorInfoChanged?.Invoke(senderType, errorInfo);
        }

        public event EventString PasswordStrengthChanged;

        private void OnPasswordStrengthChanged(EventStringTypes senderType, int strengthIndex) {
            PasswordStrengthChanged?.Invoke(senderType, strengthIndex);
        }

        public static event Action<long> NewUserAdded;

        public static event Action<long> SomeUserChanged;
    }

    public enum EventStringTypes {
        Login,
        Password,
        PasswordStrengthIndex
    }
}