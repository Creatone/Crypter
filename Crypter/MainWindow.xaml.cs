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
using System.IO;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Threading;
using System.ComponentModel;

namespace Crypter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Encryption encryption;
        int feedbackSize = 128;
        public List<User> selectedUsers = new List<User>();
        public User selectedUser = new User();
        private readonly BackgroundWorker workerEncrypt = new BackgroundWorker();
        private readonly BackgroundWorker workerDecrypt = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();

            encryption = new Encryption(listViewReceiver,listViewReceiverDecrypt);
            workerEncrypt.WorkerReportsProgress = true;
            workerEncrypt.DoWork += workerEncrypt_DoWork;
            workerEncrypt.RunWorkerCompleted += workerEncrypt_RunWorkerCompleted;
            workerEncrypt.ProgressChanged += workerEncrypt_ProgressChanged;

            workerDecrypt.WorkerReportsProgress = true;
            workerDecrypt.DoWork += workerDecrypt_DoWork;
            workerDecrypt.RunWorkerCompleted += workerDecrypt_RunWorkerCompleted;
            workerDecrypt.ProgressChanged += workerDecrypt_ProgressChanged;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button senderButton = sender as System.Windows.Controls.Button;

            string buttonTag = (string)senderButton.Tag;

            if (buttonTag == "encryptionIn")
                encryption.HandleFileIn(encryptFilePathIn);
            else if (buttonTag == "encryptionOut")
                encryption.HandleFileOut(encryptFilePathOut);
            else if (buttonTag == "encryptionAccept")
            {
                IsEnabled = false;

                foreach (var element in listViewReceiver.SelectedItems)
                {
                    User user = (User)element;
                    selectedUsers.Add(user);
                }

                encryption.HandleSelectedUsers(selectedUsers);
                workerEncrypt.RunWorkerAsync();
            }
            else if (buttonTag == "decryptionIn")
            {
                encryption.HandleFileIn(decryptFilePathIn);
            }
            else if (buttonTag == "decryptionOut")
                encryption.HandleFileOut(decryptFilePathOut);
            else if (buttonTag == "decryptionAccept")
            {
                IsEnabled = false;

                selectedUser = (User)listViewReceiverDecrypt.SelectedItem;

                encryption.HandleSelectedUser(selectedUser);

                encryption.HandlePassword(password.Password);

                workerDecrypt.RunWorkerAsync();
            }
            else if (buttonTag == "addRSA")
            {
                encryption.AddKeys(textBoxNewUser.Text,passwordBoxNewUser.Password);
            }
        }

        private void workerEncrypt_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;
            encryption.HandleEncryption(backgroundWorker);
        }

        public void workerEncrypt_ProgressChanged(object sender,ProgressChangedEventArgs e)
        {
            progressBarEncrypt.Value = e.ProgressPercentage;
        }

        private void workerEncrypt_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsEnabled = true;
            progressBarEncrypt.Value=0;
        }

        private void workerDecrypt_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;
            encryption.HandleDecryption(backgroundWorker);
        }

        public void workerDecrypt_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarDecrypt.Value = e.ProgressPercentage;
        }

        private void workerDecrypt_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsEnabled = true;
            progressBarDecrypt.Value = 0;
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ComboBox senderComboBox = sender as System.Windows.Controls.ComboBox;

            string comboBoxTag = (string)senderComboBox.Tag;

            if (comboBoxTag == "CipherMode")
            {
                ComboBoxItem tempItem = (ComboBoxItem)comboBoxCipherMode.SelectedItem;

                string cipherMode = (string)tempItem.Content;

                if (cipherMode == "ECB" || cipherMode == "CBC")
                {
                    comboBoxUnderBlockSize.IsEnabled = false;
                    comboBoxUnderBlockSize.Items.Clear();
                }
                else
                {
                    comboBoxUnderBlockSize.SelectedItem = null;
                    comboBoxUnderBlockSize.Items.Clear();
                    comboBoxUnderBlockSize.IsEnabled = true;
                    FillFeedbackSize();
                }

                encryption.HandleCipherMode(cipherMode);
            }
            else if (comboBoxTag == "KeySize")
            {
                ComboBoxItem tempItem = (ComboBoxItem)comboBoxKeySize.SelectedItem;

                string keySize = (string)tempItem.Content;

                encryption.HandleKeySize(keySize);

            }
            else if (comboBoxTag == "BlockSize")
            {
                ComboBoxItem tempItem = (ComboBoxItem)comboBoxBlockSize.SelectedItem;

                string blockSize = (string)tempItem.Content;
                feedbackSize = Int32.Parse(blockSize);
                FillFeedbackSize();
                encryption.HandleBlockSize(blockSize);            

            }
            else if (comboBoxTag == "UnderBlockSize")
            {
                ComboBoxItem tempItem = (ComboBoxItem)comboBoxUnderBlockSize.SelectedItem;

                if (tempItem == null)
                    return;

                string tempFeedbackSize = tempItem.Content.ToString();

                encryption.HandleFeedbackSize(tempFeedbackSize);
            }
        }

        void FillFeedbackSize()
        {
            comboBoxUnderBlockSize.Items.Clear();

            for (int i = 8; i <= feedbackSize; i += 2)
            {
                if (isPowerOfTwo(i) || i % 8 == 0)
                {
                    ComboBoxItem comboItem = new ComboBoxItem();
                    comboItem.Content = i;
                    comboBoxUnderBlockSize.Items.Add(comboItem);
                }
            }
        }

        public static bool isPowerOfTwo(int x)
        {
            while (((x % 2) == 0) && x > 1)
            {
                x /= 2;
            }
            return (x == 1);
        }
    }
}
