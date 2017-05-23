using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml;
using Org.BouncyCastle.Crypto;

namespace Crypter
{
    class Encryption
    {
        public OpenFileDialog dialogFileIn;
        public OpenFileDialog dialogFileOut;
        public const string ENCRYPTION_ALGORITHM = "AES";

        public CipherMode cipherMode;

        byte[] key;
        byte[] IV;

        int keySize = 256;
        int blockSize = 128;
        int feedbackSize = 128;

        public void HandleFileIn(System.Windows.Controls.TextBox pathTextBox)
        {
            dialogFileIn = new OpenFileDialog();

            if (dialogFileIn.ShowDialog() == DialogResult.OK)
            {
                pathTextBox.Text = dialogFileIn.FileName;
            }
        }
            
        public void HandleFileOut(System.Windows.Controls.TextBox pathTextBox)
        {
            dialogFileOut = new OpenFileDialog();
            dialogFileOut.CheckFileExists = false;

            if(dialogFileOut.ShowDialog() == DialogResult.OK)
            {
                pathTextBox.Text = dialogFileOut.FileName;
            }
        }

        public void HandleCipherMode(string tempCipherMode)
        {
            if (tempCipherMode == "ECB")
                cipherMode = CipherMode.ECB;
            else if (tempCipherMode == "CBC")
                cipherMode = CipherMode.CBC;
            else if (tempCipherMode == "CFB")
                cipherMode = CipherMode.CFB;
            else if (tempCipherMode == "OFB")
                cipherMode = CipherMode.OFB;

        }

        public void HandleKeySize(string tempKeySize)
        {
            if (tempKeySize == "256")
                keySize = 256;
            else if (tempKeySize == "192")
                keySize = 192;
            else if (tempKeySize == "128")
                keySize = 128;
        }

        public void HandleBlockSize(string tempBlockSize)
        {
            if (tempBlockSize == "256")
                blockSize = 256;
            else if (tempBlockSize == "192")
                blockSize = 192;
            else if (tempBlockSize == "128")
                blockSize = 128;
        }

        public void HandleKey(string tempKey)
        {
            key = Convert.FromBase64String(tempKey);
        }

        public bool Check()
        {
            if (dialogFileIn == null)
            {
                MessageBox.Show("Nie podałeś pliku do zaszyfrowania!");
                return false;
            }
            if (dialogFileOut == null)
            {
                MessageBox.Show("Nie podałeś pliku do zapisu!");
                return false;
            }
            if (cipherMode==0)
            {
                MessageBox.Show("Nie podałeś trybu szyfrowania!");
                return false;
            }   
            return true;
        }
        public void GenerateKey()
        {
            Random random = new Random();
            key = new byte[keySize/8];
            random.NextBytes(key);
        }
        public void HandleEncryption()
        {
            if (!Check())
                return;

            GenerateKey();

            CreateXmlHeader();

            EncryptBytes();
        }

        public void HandleDecryption()
        {
            ReadXmlHeader();
            
            DecryptBytes();

        }

        public void CreateXmlHeader()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = false;
            settings.WriteEndDocumentOnClose = true;
            using (XmlWriter write = XmlWriter.Create(dialogFileOut.FileName,settings)) 
            {
                write.WriteStartDocument();
                write.WriteStartElement("EncryptedFileHeader");
                write.WriteElementString("Algorithm", ENCRYPTION_ALGORITHM);
                write.WriteElementString("CipherMode", cipherMode.ToString());
                write.WriteElementString("BlockSize",blockSize.ToString());
                write.WriteElementString("Key", Convert.ToBase64String(key));
                write.WriteElementString("KeySize",keySize.ToString());

                if (cipherMode!=CipherMode.ECB)
                    write.WriteElementString("IV", Convert.ToBase64String(IV));

                write.WriteElementString("ApprovedUsers",null);
                write.Flush();
                write.Close();
            }

            File.AppendAllText(dialogFileOut.FileName, "\r\n");
           
        }

        public void ReadXmlHeader()
        {                                    
            using (XmlReader read = XmlReader.Create(dialogFileIn.FileName))
            {
                if (read.IsStartElement("EncryptedFileHeader"))
                {
                    read.ReadToFollowing("CipherMode");
                    read.Read();
                    HandleCipherMode(read.Value);
                    read.ReadToFollowing("BlockSize");
                    read.Read();
                    HandleBlockSize(read.Value);
                    read.ReadToFollowing("Key");
                    read.Read();
                    HandleKey(read.Value);
                    read.ReadToFollowing("KeySize");
                    read.Read();
                    HandleKeySize(read.Value);
                }

                read.Close();
            }
        }

        public int GetHeaderByteCount()
        {
            int countBytes = 0;

            BinaryReader br = new BinaryReader(File.Open(dialogFileIn.FileName, FileMode.Open));
            byte end = Convert.ToByte(10);

            while (true)
            {
                countBytes++;
                if (br.ReadByte() == end)
                    break;
            }

            br.Close();

            return countBytes;
        }

        void EncryptBytes()
        {
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Mode = cipherMode;
                rijAlg.KeySize = keySize;
                rijAlg.BlockSize = blockSize;
                rijAlg.Key = key;

                if (cipherMode!=CipherMode.ECB)
                    rijAlg.IV = IV;

                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);
                
                FileStream fsIn = new FileStream(dialogFileIn.FileName, FileMode.Open);
                FileStream fsOut = new FileStream(dialogFileOut.FileName, FileMode.Append);

                CryptoStream cs = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write);

                int data;

                while ((data = fsIn.ReadByte()) != -1)
                    cs.WriteByte((byte)data);

                fsIn.Close();
                cs.Close();
                fsOut.Close();
                
            }
        }

        void DecryptBytes()
        {

            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Mode = cipherMode;
                rijAlg.KeySize = keySize;
                rijAlg.BlockSize = blockSize;
                rijAlg.Key = key;
                if (cipherMode != CipherMode.ECB)
                    rijAlg.IV = IV;

                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                var headerOffset = GetHeaderByteCount();

                FileStream fsIn = new FileStream(dialogFileIn.FileName, FileMode.Open);
                fsIn.Seek(headerOffset,SeekOrigin.Begin);
                FileStream fsOut = new FileStream(dialogFileOut.FileName, FileMode.Create);

                CryptoStream cs = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read);

                int data;

                

                while ((data = cs.ReadByte()) != -1)
                    fsOut.WriteByte((byte)data);

                fsIn.Close();
                cs.Close();
                fsOut.Close();

            }
        }
    }
}
