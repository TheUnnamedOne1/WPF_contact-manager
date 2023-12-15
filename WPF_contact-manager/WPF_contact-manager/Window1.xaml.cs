using System;
using System.Collections.Generic;
using System.Data.SQLite;
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

namespace WPF_contact_manager
{
    public partial class Window1 : Window
    {

        public MainWindow parent;
        private ListBox lstContacts;
        private int contactIndex;  

        public Window1()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        public Window1(MainWindow parent = null, int contactIndex = -1)
        {
            InitializeComponent();
            this.parent = parent;
            this.contactIndex = contactIndex;
            LoadContactDetails();
        }


        public static SQLiteConnection GetFileDatabaseConnection()
        {
            var connection = new SQLiteConnection("Data Source=rolodex.db");
            connection.Open();
            return connection;
        }

        private void LoadContactDetails()
        {
            using (var connection = GetFileDatabaseConnection())
            {
                string sqlStatement = @"SELECT *
                            FROM Contacts
                            WHERE ID = @id";

                using (var cmd = new SQLiteCommand(sqlStatement, connection))
                {
                    cmd.Parameters.AddWithValue("@id", GetContactId(contactIndex));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            editfName.Text = (reader["fName"].ToString());
                            editlName.Text = (reader["lName"].ToString());
                            editphoneNumber.Text = (reader["phoneNumber"].ToString());
                            editemail.Text = (reader["email"].ToString());
                        }
                    }
                }
            }
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

        private void LoadContacts()
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Edit();
            Close();
        }


        public void Edit()
        {
            using (var connection = GetFileDatabaseConnection())
            {
                string sqlStatement = @"
                UPDATE Contacts
                SET fName = @fName, lName = @lName, phoneNumber = @phoneNumber, email = @email
                WHERE ID = @id";

                using (var cmd = new SQLiteCommand(sqlStatement, connection))
                {
                    cmd.Parameters.AddWithValue("@fName", editfName.Text);
                    cmd.Parameters.AddWithValue("@lName", editlName.Text);
                    cmd.Parameters.AddWithValue("@phoneNumber", editphoneNumber.Text);
                    cmd.Parameters.AddWithValue("@email", editemail.Text);
                    cmd.Parameters.AddWithValue("@id", GetContactId(parent.lstContacts.SelectedIndex));

                    cmd.ExecuteNonQuery();
                }
            }

            parent.LoadContacts();
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

    }
}
