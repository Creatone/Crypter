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

        private readonly BackgroundWorker workerEncrypt = new BackgroundWorker();
        private readonly BackgroundWorker workerDecrypt = new BackgroundWorker();


        public MainWindow()
        {
            InitializeComponent();
            encryption = new Encryption();
            workerEncrypt.DoWork += workerEncrypt_DoWork;
            workerEncrypt.RunWorkerCompleted += workerEncrypt_RunWorkerCompleted;
            workerDecrypt.DoWork += workerDecrypt_DoWork;
            workerDecrypt.RunWorkerCompleted += workerDecrypt_RunWorkerCompleted;
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
                progressBarEncrypt.IsIndeterminate = true;
                IsEnabled = false;
                workerEncrypt.RunWorkerAsync();
            }
            else if (buttonTag == "decryptionIn")
                encryption.HandleFileIn(decryptFilePathIn);
            else if (buttonTag == "decryptionOut")
                encryption.HandleFileOut(decryptFilePathOut);
            else if (buttonTag == "decryptionAccept")
            {
                progressBarDecrypt.IsIndeterminate = true;
                IsEnabled = false;
                workerDecrypt.RunWorkerAsync();
            }
        }

        private void workerEncrypt_DoWork(object sender, DoWorkEventArgs e)
        {
            encryption.HandleEncryption();
        }

        private void workerEncrypt_RunWorkerCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
            IsEnabled = true;
            progressBarEncrypt.IsIndeterminate = false;
        }

        private void workerDecrypt_DoWork(object sender, DoWorkEventArgs e)
        {
            encryption.HandleDecryption();
        }

        private void workerDecrypt_RunWorkerCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
            IsEnabled = true;
            progressBarDecrypt.IsIndeterminate = false;
        }

        private void File_Out_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialogFileOut = new OpenFileDialog();

            dialogFileOut.CheckFileExists = false;
            dialogFileOut.ValidateNames = false;

            if (dialogFileOut.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                encryptFilePathOut.Text = dialogFileOut.FileName;
            }
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

            }
        }

        void FillFeedbackSize()
        {
            comboBoxUnderBlockSize.Items.Clear();

            for (int i = 2; i <= feedbackSize; i += 2)
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
