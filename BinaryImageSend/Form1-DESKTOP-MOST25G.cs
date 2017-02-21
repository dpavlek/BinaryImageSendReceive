using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BinaryImageSend
{

    public partial class Form1 : Form
    {
        SerialPort serial1 = new SerialPort("COM1",115200,Parity.None,8,StopBits.One);
        Image normal_foto;
        Bitmap normal_foto_bit,grayscale_foto_bit;
        OpenFileDialog openFileDialog1 = new OpenFileDialog();
        long StreamSize = 0;

        public Form1()
        {
            InitializeComponent();
            this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            progressBar1.Visible = false;
            ImagePathLabel.Visible = false;
            SendButton.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Bitmap images|*.bmp";
            openFileDialog1.Title = "Select a Bitmap image";
            switch(openFileDialog1.ShowDialog())
            {
                case (DialogResult.OK):
                normal_foto = Image.FromFile(openFileDialog1.FileName);
                normal_foto_bit = new Bitmap(normal_foto);
                pictureBox1.Image = normal_foto_bit;
                ImagePathLabel.Text = openFileDialog1.FileName;
                    break;
                case (DialogResult.Cancel):
                    MessageBox.Show("Potrebno je učitati sliku za binarizaciju.");
                    break;
            }
            ImagePathLabel.Visible = true;
            SendButton.Enabled = true;
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            progressBar1.Maximum = 100;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
            progressBar1.Visible = true;
            try
            {
                serial1.Open();
            }
            catch(Exception err)
            {
                MessageBox.Show("Error:\n " + err + "\n\n Serial Port se ne može spojiti.");
            }
            StreamSize = 0;
            try
            {
                grayscale_foto_bit = MakeGrayscale(normal_foto_bit);
            }
            catch(Exception err)
            {
                MessageBox.Show("Error:\n " + err + "!\n\n P.S. Vjerojatno nema učitane slike, pa zato baca error.");
            }
            pictureBox1.Image=grayscale_foto_bit;
            var ms = new MemoryStream();
            grayscale_foto_bit.Save(ms, ImageFormat.Bmp);
            ms.Position = 0;
            StreamSize = ms.Length;
            Console.WriteLine(StreamSize);
            try
            {
                for (int i = 0; i < StreamSize; i++)
                {
                    serial1.Write(ms.ToArray(), i, 1);
                }
                progressBar1.Value = 50;
            }
            catch(Exception err)
            {
                MessageBox.Show("Error: " + err);
            }
        }

        public static Bitmap MakeGrayscale(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }       
    }
}
