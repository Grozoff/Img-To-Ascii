﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImgToAscii
{
    public partial class Form1 : Form
    {
        private static int _quality;
        private static double _widthOffset;
        private Bitmap _bitmap;
        private static string _filename;

        public Form1()
        {
            InitializeComponent();
        }

        private void ImagePreprocessing(Bitmap bitmap)
        {
            bitmap = ResizeBitmap(bitmap);
            bitmap.ToGrayScale();

            var converter = new BitmapToAsciiConverter(bitmap);

            var rows = CheckInvertColor(converter);

            ThreadSafeInvoke(() =>
            {
                progressBar1.Maximum = rows.GetLength(0);
            });
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                ThreadSafeInvoke(() =>
                {
                    richTextBox1.Text += new string(rows[i]) + Environment.NewLine;
                });
                ThreadSafeInvoke(() =>
                {
                    UpdateProgressBar(i);
                });
            }
            if (progressBar1.Value == rows.GetLength(0) - 1)
            {
                ThreadSafeInvoke(() =>
                {
                    progressBar1.Value = 0;
                    label5Complete.Text = "Complete!";
                });
            }
        }

        private void UpdateProgressBar(int i)
        {
            if (i == progressBar1.Maximum)
            {
                progressBar1.Maximum = i + 1;
                progressBar1.Value = i + 1;
                progressBar1.Maximum = i;
            }
            else
            {
                progressBar1.Value = i + 1;
            }
            progressBar1.Value = i;
        }

        private char[][] CheckInvertColor(BitmapToAsciiConverter converter)
        {

            if (checkBoxInvert.Checked)
            {
                ThreadSafeInvoke(() =>
                { 
                    richTextBox1.BackColor = Color.Black; 
                    richTextBox1.ForeColor = Color.White;
                });

                return converter.Convert();
            }
            else
            {
                ThreadSafeInvoke(() =>
                { 
                    richTextBox1.BackColor = Color.White;
                    richTextBox1.ForeColor = Color.Black;
                });
                return converter.ConvertInvert();
            }
        }

        private async void StartConvert()
        {
            if (_bitmap == null)
                return;

            buttonConvert.Enabled = false;
            richTextBox1.Clear();
            richTextBox1.SelectionAlignment = HorizontalAlignment.Center;

            label5Complete.Text = string.Empty;
            progressBar1.Visible = true;

            _quality = (int)numericUpDownQuality.Value * 100;
            _widthOffset = (double)numericUpDownWidthOffset.Value;

            await Task.Run(() =>
            {
                ImagePreprocessing(_bitmap);
            });

            saveToolStripMenuItem.Enabled = true;
            buttonConvert.Enabled = true;
            progressBar1.Visible = false;
        }
        public void ThreadSafeInvoke(Action action)
        {
            try
            {
                if (!InvokeRequired)
                    action();// Execute action.
                else
                    Invoke(action, new object[0]);
            }
            catch { }
        }
        private static Bitmap ResizeBitmap(Bitmap bitmap)
        {
            var newHeight = bitmap.Height / _widthOffset * _quality / bitmap.Width;

            if (bitmap.Width > _quality || bitmap.Height > newHeight)
                bitmap = new Bitmap(bitmap, new Size(_quality, (int)newHeight));

            return bitmap;
        }

        private static void openSavedFileInExplorer(string path)
        {
            if (File.Exists(path))
                Process.Start("explorer.exe", "/select, " + path);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartConvert();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _bitmap = new Bitmap(openFileDialog1.FileName);
                label4.Text = "Original image";
                pictureBox1.Image = _bitmap;
                _filename = Path.GetFileNameWithoutExtension(openFileDialog1.FileName) + " ASCII.txt";
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = _filename;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var fileName = saveFileDialog1.FileName;
                File.WriteAllText(fileName, richTextBox1.Text);
                saveFileDialog1.FileName = string.Empty;
                openSavedFileInExplorer(fileName);
            }
        }
    }
}
