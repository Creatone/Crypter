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

namespace Crypter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void File_In_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialogFileIn = new OpenFileDialog();
            if (dialogFileIn.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                encryptFilePathIn.Text = dialogFileIn.FileName;
            }
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

        private void File_Public_Keys_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Encrypt_Button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Decrypt_Button_Click(object sender, RoutedEventArgs e)
        {
         
        }
    }
}
