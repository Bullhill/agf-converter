using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Converter
{
    public partial class MainWindow : Window
    {
        public List<SelectedFile> SelectedFiles = new List<SelectedFile>();
        public string OutputFolder = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
        }
        public void updateSelectedFilesList()
        {
            FileListDataGrid.Items.Clear();
            foreach (SelectedFile file in SelectedFiles.OrderBy(w => w.FieldName))
                FileListDataGrid.Items.Add(file);
        }
        private void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFile = new Microsoft.Win32.OpenFileDialog();
            openFile.RestoreDirectory = true;
            openFile.Filter = "agf files (*.agf)|*.agf|All files (*.*)|*.*";
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;
            openFile.Multiselect = true;
            openFile.ShowDialog();

            foreach(string file in openFile.FileNames)
            {
                if(!SelectedFiles.Where(w => w.FileName == file).Any())
                    SelectedFiles.Add(new SelectedFile(file));
            }
            updateSelectedFilesList();
        }
        private void SelectedFilesListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach(SelectedFile item in FileListDataGrid.SelectedItems)
            {
                SelectedFiles.Remove(SelectedFiles.Where(x => x.FileName == item.FileName).First());
            }
            updateSelectedFilesList();
        }
        private void FolderSelectorButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFolder = dialog.SelectedPath;
                FolderSelectorText.Text = OutputFolder;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFiles.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("No files selected!", "No files selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (EncryptionKey.Text.Length == 0)
            {
                System.Windows.Forms.MessageBox.Show("Encryption key missing", "No key", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!convert.TryStringToByteArray(EncryptionKey.Text))
            {
                System.Windows.Forms.MessageBox.Show("Encryption key not valid", "Invalid key", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!Directory.Exists(OutputFolder))
            {
                System.Windows.Forms.MessageBox.Show("Output folder does not exsists", "No missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            foreach(SelectedFile file in SelectedFiles)
            {
                if (File.Exists(file.FileName))
                {

                    if (Directory.Exists(OutputFolder + "/" + file.FileFirstName))
                    {
                        if (System.Windows.Forms.MessageBox.Show($"Outputfolder {file.FileFirstName} allready exsist, Do you want to overwrite?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.No)
                        {
                            return;
                        }
                        Directory.Delete(OutputFolder + "/" + file.FileFirstName, true);
                    }
                    file.Status = SelectedFile.StatusList.UnZipping;
                    updateSelectedFilesList();
                    convert.Unzip(file.FileName, OutputFolder + "/" + file.FileFirstName);
                    file.Status = SelectedFile.StatusList.UnZipped;
                    updateSelectedFilesList();

                    file.Status = SelectedFile.StatusList.DeCrypting;
                    updateSelectedFilesList();
                    if(!convert.Decrypt(OutputFolder + "/" + file.FileFirstName, EncryptionKey.Text))
                    {
                        file.Status = SelectedFile.StatusList.DeCryptFailed;
                        return;
                    }
                    file.Status = SelectedFile.StatusList.DeCrypted;
                    updateSelectedFilesList();

                    file.DecodeGeometry(OutputFolder + "/" + file.FileFirstName + "/" + file.FileFirstName + ".xml");
                    updateSelectedFilesList();

                    file.CreateKML(OutputFolder + "/" + file.FieldName + ".kml");
                    updateSelectedFilesList();
                    file.Status = SelectedFile.StatusList.Complete;
                    updateSelectedFilesList();
                }
            }
            
        }
    }
}
