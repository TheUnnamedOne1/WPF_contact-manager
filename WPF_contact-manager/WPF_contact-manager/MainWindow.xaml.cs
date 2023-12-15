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
using System.Data.SQLite;
using WPF_contact_manager;
using Microsoft.Win32;


namespace WPF_contact_manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int currentContactIndex = 0;


        public MainWindow()
        {
            InitializeComponent();

            InitializeDatabase();
            
            LoadContacts();

        }

        public static SQLiteConnection GetFileDatabaseConnection()
        {
            var connection = new SQLiteConnection("Data Source=rolodex.db");
            connection.Open();
            return connection;
        }

        private void InitializeDatabase()
        {
            using (var connection = GetFileDatabaseConnection())
            {
                string sqlStatement = @"
                    SELECT count(name) 
                    FROM sqlite_master
                    WHERE (
                        type = 'table' AND name = 'Contacts'
                    )";
                var cmdCheck = new SQLiteCommand(sqlStatement, connection);

                if ((long)cmdCheck.ExecuteScalar() == 0)
                {
                    using (var cmd = new SQLiteCommand(connection))
                    {
                        sqlStatement = @"
                            CREATE TABLE Contacts (
                                ID INTEGER PRIMARY KEY,
                                fName VARCHAR2(20),
                                lName VARCHAR2(20),
                                phoneNumber VARCHAR2(20),
                                email VARCHAR2(30)
                             )";

                        cmd.CommandText = sqlStatement;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        
        public void LoadContacts()
        {
            lstContacts.Items.Clear();

            using (var connection = GetFileDatabaseConnection())
            {
                var command = new SQLiteCommand(@"SELECT *
                                                FROM Contacts", connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lstContacts.Items.Add(reader["fName"].ToString());

                    }
                }
            }

            if (lstContacts.Items.Count > 0)
            {
                lstContacts.SelectedIndex = 0;
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (currentContactIndex < lstContacts.Items.Count - 1)
            {
                currentContactIndex++;
                lstContacts.SelectedIndex = currentContactIndex;
            }
        }


        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if(currentContactIndex > 0)
            {
                currentContactIndex--;
                lstContacts.SelectedIndex = currentContactIndex;
            }
        }

        private void AddContact(string fName, string lName, string phoneNumber, string email)
        {

            using (var connection = GetFileDatabaseConnection())
            {
                string sqlStatement = @"
                    INSERT INTO Contacts (fName, lName, phoneNumber, email)
                    VALUES (@fName, @lName, @phoneNumber, @email)";

                using (var cmd = new SQLiteCommand(sqlStatement, connection))
                {
                    cmd.Parameters.AddWithValue("@fName", fName);
                    cmd.Parameters.AddWithValue("@lName", lName);
                    cmd.Parameters.AddWithValue("@phoneNumber", phoneNumber);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.ExecuteNonQuery();
                }
            }

            LoadContacts();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            
            string newfName = fName.Text;
            string newlName = lName.Text;
            string newPhone = phoneNumber.Text;
            string newEmail = email.Text;
            
            AddContact(newfName, newlName, newPhone, newEmail);

            fName.Text = "";
            lName.Text = "";
            phoneNumber.Text = "";
            email.Text = "";
            
        }

        private void DeleteFromId(int contactId)
        {
            using (var connection = GetFileDatabaseConnection())
            {
                string sqlStatement = "DELETE FROM Contacts WHERE ID = @contactId";
                using (var cmd = new SQLiteCommand(sqlStatement, connection))
                {
                    cmd.Parameters.AddWithValue("@contactId", contactId);
                    cmd.ExecuteNonQuery();
                }
            }

            LoadContacts();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (lstContacts.SelectedIndex >= 0)
            {

                int contactId = GetContactId(lstContacts.SelectedIndex);

                DeleteFromId(contactId);

                if (currentContactIndex >= lstContacts.Items.Count)
                {
                    currentContactIndex = lstContacts.Items.Count - 1;
                }

                lstContacts.SelectedIndex = currentContactIndex;
            }
        }

        private int GetContactId(int selectedIndex)
        {
            using (var connection = GetFileDatabaseConnection())
            {
                var command = new SQLiteCommand("SELECT ID FROM Contacts LIMIT 1 OFFSET @index", connection);
                command.Parameters.AddWithValue("@index", selectedIndex);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (lstContacts.SelectedIndex >= 0)
            {
                Window1 w1 = new Window1(this, lstContacts.SelectedIndex);
                w1.ShowDialog();
                LoadContacts();
            }
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            Window2 w2 = new Window2();
            using (var connection = GetFileDatabaseConnection())
            {
                string sqlStatement = @"SELECT *
                                        FROM Contacts
                                        WHERE ID = @id";

                using (var cmd = new SQLiteCommand(sqlStatement, connection))
                {

                    cmd.Parameters.AddWithValue("@id", GetContactId(lstContacts.SelectedIndex));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            w2.viewfName.Text = (reader["fName"].ToString());
                            w2.viewlName.Text = (reader["lName"].ToString());
                            w2.viewphoneNumber.Text = (reader["phoneNumber"].ToString());
                            w2.viewemail.Text = (reader["email"].ToString());

                        }
                    }
                }
            }

            w2.Show();
            LoadContacts(); 
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text Files|*.csv",
                Title = "Save Database",
                DefaultExt = "csv",
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                ExportDatabaseToText(filePath);

                MessageBox.Show("Database exported successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportDatabaseToText(string filePath)
        {
            using (var connection = GetFileDatabaseConnection())
            {
                var command = new SQLiteCommand("SELECT * FROM Contacts", connection);

                using (var reader = command.ExecuteReader())
                {
                    using (var writer = new System.IO.StreamWriter(filePath))
                    {
                        writer.WriteLine("ID,First Name,Last Name,Phone,Email");

                        while (reader.Read())
                        {
                            writer.WriteLine($"{reader["ID"]},{reader["fName"]},{reader["lName"]},{reader["phoneNumber"]},{reader["email"]}");
                        }
                    }
                }
            }
        }
    }
}



