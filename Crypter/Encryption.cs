using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml;
using Org.BouncyCastle.Crypto;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections;
using System.Text;

namespace Crypter
{
    // Each file is encrypted with the Rijndael algorithm, whose key is encrypted by the RSA.
    // Application provide the recipients.
    public class User
    {
        public string Name = "";
        public string SessionKey = "";
        public byte[] encryptedKey;

        public override string ToString()
        {
            return Name;
        }
    }

    class Encryption
    {

        OpenFileDialog dialogFileIn;
        OpenFileDialog dialogFileOut;
        const string ENCRYPTION_ALGORITHM = "AES(Rijndael)";
        CipherMode cipherMode;

        byte[] key;
        byte[] IV;

        int keySize = 256;
        int blockSize = 128;
        int feedbackSize = 128;

        PaddingMode paddingMode = PaddingMode.PKCS7;

        List<User> receiverList = new List<User>();
        List<User> receiverDecryptList = new List<User>();
        List<User> selectedUsers;
        User selectedUser;


        // Paths with public and private RSA keys of receivers.
        string receiversPath = Path.GetDirectoryName(Application.ExecutablePath) + "\\public\\";
        string receiversDecryptPath = Path.GetDirectoryName(Application.ExecutablePath) + "\\private\\";

        string password = "";

        public Encryption(System.Windows.Controls.ListView listViewReceiver, System.Windows.Controls.ListView listViewReceiverDecrypt)
        {
            // Create directories for RSA keys if they doesn't exists.
            if (!Directory.Exists(receiversPath))
                Directory.CreateDirectory(receiversPath);
            if (!Directory.Exists(receiversDecryptPath))
                Directory.CreateDirectory(receiversDecryptPath);

            AddReceivers();

            listViewReceiver.ItemsSource = receiverList;
            listViewReceiverDecrypt.ItemsSource = receiverDecryptList;
        }

        // Interaction with GUI.
        public void HandleSelectedUsers(List<User> tempSelectedUsers)
        {
            selectedUsers = tempSelectedUsers;
        }

        // Interaction with GUI.
        public void HandleFileIn(System.Windows.Controls.TextBox pathTextBox)
        {
            dialogFileIn = new OpenFileDialog();

            if (dialogFileIn.ShowDialog() == DialogResult.OK)
            {
                pathTextBox.Text = dialogFileIn.FileName;
            }
            else
                return;

            AddReceiversDecrypt(dialogFileIn.FileName);

        }

        // Interaction with GUI.
        public void HandleFileOut(System.Windows.Controls.TextBox pathTextBox)
        {
            dialogFileOut = new OpenFileDialog();
            dialogFileOut.CheckFileExists = false;

            if (dialogFileOut.ShowDialog() == DialogResult.OK)
            {
                pathTextBox.Text = dialogFileOut.FileName;
            }
            else
                return;
        }

        // Interaction with GUI.
        public void HandleCipherMode(string tempCipherMode)
        {
            if (tempCipherMode == "ECB")
                cipherMode = CipherMode.ECB;
            else if (tempCipherMode == "OFB")
                cipherMode = CipherMode.OFB;
            else if (tempCipherMode == "CFB")
                cipherMode = CipherMode.CFB;
            else if (tempCipherMode == "CBC")
                cipherMode = CipherMode.CBC;

        }

        // Interaction with GUI.
        public void HandleSelectedUser(User tempSelectedUser)
        {
            selectedUser = tempSelectedUser;
        }

        // Each receiver had encrypted private key with own password hash(SHA-256) as a session key for Rijndael ECB 256 bits encryption.
        public void AddKeys(string name, string password)
        {
            // Generate new RSA.
            var rsa = new RSACryptoServiceProvider();

            // Write public key to folder with public keys.
            File.WriteAllText(receiversPath + name + ".pub", rsa.ToXmlString(false));
            
            // Write temporary private key.
            File.WriteAllText(receiversDecryptPath + name + "_temp", rsa.ToXmlString(true));

            // Update list.
            AddReceivers();

            // Encrypt private key.
            using (Rijndael rijAlg = Rijndael.Create())
            {
                byte[] mybytes = new byte[32];
                for (int i = 0; i < 32; i++)
                    mybytes[i] = 0;
                Array.Copy(GetHash(password), mybytes, 32);

                rijAlg.Mode = CipherMode.ECB;
                rijAlg.Key = mybytes;

                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                FileStream fsIn = new FileStream(receiversDecryptPath + name + "_temp", FileMode.Open);
                FileStream fsOut = new FileStream(receiversDecryptPath + name, FileMode.CreateNew);

                CryptoStream cs = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write);

                int data;

                while ((data = fsIn.ReadByte()) != -1)
                {
                    cs.WriteByte((byte)data);
                }
                fsIn.Close();
                cs.Close();
                fsOut.Close();

                // Delete temporary key.
                File.Delete(receiversDecryptPath + name + "_temp");
            }
        }

        // Interaction with GUI or encrypted file header.
        public void HandleKeySize(string tempKeySize)
        {
            keySize = int.Parse(tempKeySize);
        }

        // Interaction with GUI or encrypted file header.
        public void HandleBlockSize(string tempBlockSize)
        {
            blockSize = int.Parse(tempBlockSize);
        }

        // Interaction with GUI or encrypted file header.
        public void HandleFeedbackSize(string tempFeedbackSize)
        {
            feedbackSize = int.Parse(tempFeedbackSize);
        }

        // Interaction with encrypted file header.
        public void HandleIV(string tempIV)
        {
            IV = Convert.FromBase64String(tempIV);
        }

        // Interaction with encrypted file header.
        public void HandleKey(string tempKey)
        {
            key = Convert.FromBase64String(tempKey);
        }

        // Checking fields before encrypt/decrypt.
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
            if (cipherMode == 0)
            {
                MessageBox.Show("Nie podałeś trybu szyfrowania!");
                return false;
            }
            return true;
        }

        // Add receivers from path.
        public void AddReceivers()
        {
            receiverList.Clear();
            string[] receivers = Directory.GetFiles(receiversPath);

            foreach (var receiver in receivers)
            {
                User newUser = new User() { Name = Path.GetFileNameWithoutExtension(receiver), SessionKey = File.ReadAllText(receiver) };
                receiverList.Add(newUser);
            }

            ICollectionView view = CollectionViewSource.GetDefaultView(receiverList);
            view.Refresh();
        }

        // Add receivers from encrypted file xml.
        public void AddReceiversDecrypt(string decryptPath)
        {
            receiverDecryptList.Clear(); 

            ICollectionView view = CollectionViewSource.GetDefaultView(receiverDecryptList);
            XmlReader read = XmlReader.Create(decryptPath);

            // Try to read receiver from file header
            try
            {
                while (read.ReadToFollowing("User"))
                {
                    string tempName = "";

                    read.ReadToFollowing("Name");
                    read.Read();
                    tempName = read.Value;

                    User newUser;

                    if (File.Exists(receiversDecryptPath + tempName))
                    {
                        newUser = new User() { Name = Path.GetFileNameWithoutExtension(tempName), encryptedKey = File.ReadAllBytes(receiversDecryptPath + tempName) };
                    }
                    else
                    {
                        newUser = new User() { Name = Path.GetFileNameWithoutExtension(tempName), encryptedKey = null };
                    }
                    receiverDecryptList.Add(newUser);
                }
            }
            catch
            {
                view.Refresh();
            }

            view.Refresh();
        }

        public void GenerateKeyAndIV()
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            key = new byte[keySize / 8];
            random.NextBytes(key);
            IV = new byte[blockSize / 8];
            random.NextBytes(IV);
        }

        // Interaction with GUI.
        public void HandlePassword(string tempPassword)
        {
            password = tempPassword;
        }

        // Interaction with GUI, worker for updating progress bar.
        public void HandleEncryption(BackgroundWorker worker)
        {
            // Check if all fields are typed.
            if (!Check())
                return;

            paddingMode = PaddingMode.PKCS7;
            GenerateKeyAndIV();
            CreateXmlHeader();
            EncryptBytes(worker);
        }

        // Interaction with GUI.
        public void HandleDecryption(BackgroundWorker worker)
        {
            paddingMode = PaddingMode.PKCS7;
            ReadXmlHeader();

            DecryptBytes(worker);

        }

        // Get password SHA-256.
        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.ASCII.GetBytes(inputString));
        }

        // Receiver private key is encrypted with Rijndael 256 bits ECB and password SHA-256 as key.
        public void HandlePassword()
        {
            string temp = selectedUser.Name;
            byte[] tempByte = selectedUser.encryptedKey;
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.KeySize = 256;
                rijAlg.BlockSize = 128;
                rijAlg.Mode = CipherMode.ECB;

                byte[] encPassword = GetHash(password);
                rijAlg.Key = encPassword;

                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                byte[] readFromFile = File.ReadAllBytes(receiversDecryptPath + selectedUser.Name);
                byte[] score = null;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(readFromFile, 0, readFromFile.Length);
                    }

                    score = ms.ToArray();
                }

                selectedUser.SessionKey = System.Text.Encoding.UTF8.GetString(score);
            }
        }

        public void CreateXmlHeader()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true; // xml formatting
            settings.WriteEndDocumentOnClose = true;
            using (XmlWriter write = XmlWriter.Create(dialogFileOut.FileName, settings))
            {
                write.WriteStartDocument();
                write.WriteStartElement("EncryptedFileHeader");
                write.WriteElementString("Algorithm", ENCRYPTION_ALGORITHM);
                write.WriteElementString("CipherMode", cipherMode.ToString());
                write.WriteElementString("KeySize", keySize.ToString());
                write.WriteElementString("BlockSize", blockSize.ToString());

                if (cipherMode != CipherMode.ECB && cipherMode != CipherMode.CBC)
                    write.WriteElementString("FeedbackSize", feedbackSize.ToString());

                if (cipherMode != CipherMode.ECB)
                    write.WriteElementString("IV", Convert.ToBase64String(IV));

                write.WriteStartElement("ApprovedUsers");

                foreach (var element in selectedUsers)
                {
                    byte[] sessionKey = RSAPublicKeyEncryption(element.SessionKey);
                    write.WriteStartElement("User");
                    write.WriteElementString("Name", element.Name);
                    write.WriteElementString("SessionKey", Convert.ToBase64String(sessionKey));
                    write.WriteEndElement();
                }
                write.WriteEndDocument();
                write.Flush();
                write.Close();
            }
            // New line and EXT character append.
            File.AppendAllText(dialogFileOut.FileName, "\n" + Char.ToString((char)3));

        }

        public byte[] RSAPublicKeyEncryption(string publicKey)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKey);
            var encryptedByteArray = rsa.Encrypt(key, false).ToArray();
            return encryptedByteArray;
        }

        public byte[] RSAPrivateKeyDecryption(string privateKey)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);
            var decryptedByteArray = rsa.Decrypt(key, false);
            return decryptedByteArray;
        }

        // Read xml header, if typed user not match to choosen, decrypt with typed password SHA-256.
        public void ReadXmlHeader()
        {
            using (XmlReader read = XmlReader.Create(dialogFileIn.FileName))
            {
                if (read.IsStartElement("EncryptedFileHeader"))
                {
                    read.ReadToFollowing("CipherMode");
                    read.Read();
                    HandleCipherMode(read.Value);
                    read.ReadToFollowing("KeySize");
                    read.Read();
                    HandleKeySize(read.Value);
                    read.ReadToFollowing("BlockSize");
                    read.Read();
                    HandleBlockSize(read.Value);

                    if (cipherMode == CipherMode.CFB || cipherMode == CipherMode.OFB)
                    {
                        read.ReadToFollowing("FeedbackSize");
                        read.Read();
                        HandleFeedbackSize(read.Value);
                    }
                    if (cipherMode != CipherMode.ECB)
                    {
                        read.ReadToFollowing("IV");
                        read.Read();
                        HandleIV(read.Value);
                    }
                    try
                    {
                        while (read.ReadToFollowing("User"))
                        {
                            User temp = new User();
                            read.ReadToFollowing("Name");
                            read.Read();

                            if (read.Value != selectedUser.Name)
                                continue;

                            read.ReadToFollowing("SessionKey");
                            read.Read();
                            HandleKey(read.Value);
                            break;
                        }
                        HandlePassword();
                        key = RSAPrivateKeyDecryption(selectedUser.SessionKey);
                    }
                    catch
                    {
                        byte[] encPassword = GetHash(password);

                        key = encPassword;

                        paddingMode = PaddingMode.Zeros;
                    }

                }

                read.Close();
            }
        }

        // Count bytes to avoid header.
        public int GetHeaderByteCount()
        {
            int countBytes = 0;

            BinaryReader br = new BinaryReader(File.Open(dialogFileIn.FileName, FileMode.Open));
            byte end = Convert.ToByte(3);

            while (true)
            {
                countBytes++;
                if (br.ReadByte() == end)
                    break;
            }

            br.Close();

            return countBytes;
        }

        // Worker update progress bar.
        void EncryptBytes(BackgroundWorker worker)
        {
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Mode = cipherMode;
                rijAlg.KeySize = keySize;
                rijAlg.BlockSize = blockSize;
                rijAlg.Key = key;

                if (cipherMode == CipherMode.CFB || cipherMode == CipherMode.OFB)
                    rijAlg.FeedbackSize = feedbackSize;
                if (cipherMode == CipherMode.OFB)
                    rijAlg.Mode = CipherMode.CFB;
                if (cipherMode != CipherMode.ECB)
                    rijAlg.IV = IV;

                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                FileStream fsIn = new FileStream(dialogFileIn.FileName, FileMode.Open);
                FileStream fsOut = new FileStream(dialogFileOut.FileName, FileMode.Append);

                CryptoStream cs = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write);

                int data;
                int progress = 0;
                long gap = fsIn.Length / 100;
                while ((data = fsIn.ReadByte()) != -1)
                {
                    cs.WriteByte((byte)data);
                    if (fsIn.Position % gap == 0)
                    {
                        progress++;
                        worker.ReportProgress(progress);
                    }
                }
                worker.ReportProgress(100);
                fsIn.Close();
                cs.Close();
                fsOut.Close();

            }
        }

        // Worker update progress bar.
        void DecryptBytes(BackgroundWorker worker)
        {
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.Mode = cipherMode;
                rijAlg.KeySize = keySize;
                rijAlg.BlockSize = blockSize;
                rijAlg.Key = key;
                rijAlg.Padding = paddingMode;
                if (cipherMode == CipherMode.CFB || cipherMode == CipherMode.OFB)
                    rijAlg.FeedbackSize = feedbackSize;
                if (cipherMode == CipherMode.OFB)
                    rijAlg.Mode = CipherMode.CFB;
                if (cipherMode != CipherMode.ECB)
                    rijAlg.IV = IV;

                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                var headerOffset = GetHeaderByteCount();
                FileStream fsIn = new FileStream(dialogFileIn.FileName, FileMode.Open);
                fsIn.Seek(headerOffset, SeekOrigin.Begin);
                FileStream fsOut = new FileStream(dialogFileOut.FileName, FileMode.Create);
                CryptoStream cs = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read);

                int data;
                int progress = 0;
                long gap = fsIn.Length / 100;

                while ((data = cs.ReadByte()) != -1)
                {
                    fsOut.WriteByte((byte)data);
                    if (fsOut.Position % gap == 0)
                    {
                        progress++;
                        worker.ReportProgress(progress);
                    }
                }

                worker.ReportProgress(100);

                cs.Close();
                fsIn.Close();
                fsOut.Close();

            }
        }
    }
}
