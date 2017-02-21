/*Authors:
 * Vladimir Anić
 * Gabrijela Kramar
 * Daniel Pavleković
 * Matija Tivanovac
 * 
 * Name: Adaptivna Binarizacija Slike
 * 
 * Dizajn Računalnih Sustava KV
 * FERIT
 * 2016./2017. */

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
using System.Runtime.InteropServices;
//Sve radi, ne diraj nista!
//JA RADIM I IMAM X!!!!
namespace BinaryImageSend
{

    public partial class Form1 : Form
    {
        StringBuilder sb = new StringBuilder();
        SerialPort serial1 = new SerialPort();
        Image normal_foto,return_foto;
        Bitmap normal_foto_bit, grayscale_foto_bit;
        OpenFileDialog openFileDialog1 = new OpenFileDialog();
        long StreamSize = 0;
        byte[] image, image_convert;
        int send_pointer=0;
        MemoryStream ms = new MemoryStream();
        System.IO.StreamWriter file = new System.IO.StreamWriter("log"+DateTime.Now.ToString("dd-MM-yyyy") +".txt",true);

        public Form1()
        {
            InitializeComponent();
            comboBox1.DropDownStyle=ComboBoxStyle.DropDownList;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            file.WriteLine("New Session - " + DateTime.Now + System.Environment.NewLine + System.Environment.NewLine);
            progressBar1.Visible = false;
            ImagePathLabel.Visible = false;
            SendButton.Enabled = false;
            comboBox2.Items.Add("9600");
            comboBox2.Items.Add("14400");
            comboBox2.Items.Add("19200");
            comboBox2.Items.Add("28800");
            comboBox2.Items.Add("38400");
            comboBox2.Items.Add("56000");
            comboBox2.Items.Add("57600");
            comboBox2.Items.Add("115200");
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.SelectedIndex = 0;
            serial1.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            ImageTestBtn.Enabled = false;
            DiscBtn.Enabled = false;
            serial1.DataBits = 8;
            serial1.StopBits = StopBits.One;
            serial1.Parity = Parity.None;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.Filter = "Image files (*.jpg, *.jpeg, *.bmp, *.png) | *.jpg; *.jpeg; *.bmp; *.png";
                openFileDialog1.Title = "Select an image";
                switch (openFileDialog1.ShowDialog())
                {
                    case (DialogResult.OK):
                        normal_foto = Image.FromFile(openFileDialog1.FileName);
                        normal_foto_bit = new Bitmap(normal_foto);
                        pictureBox1.Image = normal_foto_bit;
                        ImagePathLabel.Text = openFileDialog1.FileName;
                        break;
                    case (DialogResult.Cancel):
                        break;
                }
                grayscale_foto_bit = MakeGrayscale(normal_foto_bit);
                pictureBox1.Image = grayscale_foto_bit;
                grayscale_foto_bit.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                StreamSize = ms.Length;
                ImagePathLabel.Visible = true;
                SendButton.Enabled = true;
                ImageTestBtn.Enabled = true;
                InfoBox.AppendText(normal_foto.Width + "x" + normal_foto.Height + System.Environment.NewLine);
                normal_foto.Dispose();
                normal_foto_bit.Dispose();
            }
            catch(Exception err)
            {
                file.WriteLine(DateTime.Now + " Error: " + err + System.Environment.NewLine);
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            send_pointer = 0;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
            progressBar1.Visible = true;
            ConnectToPort();
            ms.Position = 0;
            image = ms.ToArray();
            sb.Clear();
            image_convert = new byte[ms.Length-image[10]];
            int sirina = grayscale_foto_bit.Width;
            int visina = grayscale_foto_bit.Height;
            byte[] stupci = Encoding.ASCII.GetBytes(grayscale_foto_bit.Width.ToString());
            byte[] retci = Encoding.ASCII.GetBytes(grayscale_foto_bit.Height.ToString());
            int pocetak = image[10];
            int starting_point=0;
            for (int i = pocetak; i < (ms.Length - pocetak); i++)
            {
                image_convert[starting_point] = image[i];
                starting_point++;
            }
            int velicina = sirina * visina * 4;
            InfoBox.AppendText("Size:" + velicina.ToString() + " byte" + System.Environment.NewLine);
            progressBar1.Maximum = velicina;
            progressBar1.Value = 0;
            try
            {
                serial1.Write(stupci, 0, 3);
                serial1.Write(retci, 0, 3);
                for (int i = 0; i < velicina; i+=4)
                {
                        serial1.Write(image_convert, i, 1);
                        progressBar1.Value=i;
                        send_pointer++;
                }
                progressBar1.Value = progressBar1.Maximum;
                InfoBox.AppendText("Sent!" + System.Environment.NewLine);
                }
            catch (Exception err)
            {
                file.WriteLine(DateTime.Now + "Error:" + err + System.Environment.NewLine);
            }
        }

        public static Bitmap MakeGrayscale(Bitmap original)
        {
            /*Source: https://web.archive.org/web/20130111215043/http://www.switchonthecode.com/tutorials/csharp-tutorial-convert-a-color-image-to-grayscale */
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            serial1.PortName = this.comboBox1.GetItemText(this.comboBox1.SelectedItem);
            InfoBox.AppendText("Port:" + serial1.PortName + System.Environment.NewLine);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox2.SelectedIndex)
            {
                case 0:
                    serial1.BaudRate = 9600;
                    break;
                case 1:
                    serial1.BaudRate = 14400;
                    break;
                case 2:
                    serial1.BaudRate = 19200;
                    break;
                case 3:
                    serial1.BaudRate = 28800;
                    break;
                case 4:
                    serial1.BaudRate = 38400;
                    break;
                case 5:
                    serial1.BaudRate = 56000;
                    break;
                case 6:
                    serial1.BaudRate = 57600;
                    break;
                case 7:
                    serial1.BaudRate = 115200;
                    break;
            }
            InfoBox.AppendText("Baudrate:" + serial1.BaudRate + System.Environment.NewLine);
        }

        private void ConnectToPort()
        {
            try
            {
                if (!serial1.IsOpen)
                {
                    serial1.Open();
                    comboBox1.Enabled = false;
                    DiscBtn.Enabled = true;
                    InfoBox.AppendText("Connected!" + System.Environment.NewLine);
                }
            }
            catch (Exception err)
            {
                file.WriteLine(DateTime.Now + "Error: " + err + "\n\n Serial Port se ne može spojiti." + System.Environment.NewLine);
            }
        }

        private void ImageTestBtn_Click(object sender, EventArgs e)
        {
                ms.Position = 0;
                InfoBox.AppendText("Stream size: " + StreamSize + System.Environment.NewLine);
                byte[] stupci = Encoding.ASCII.GetBytes(grayscale_foto_bit.Width.ToString());
                byte[] retci = Encoding.ASCII.GetBytes(grayscale_foto_bit.Height.ToString());
                byte[] slika = ms.ToArray();
            try
            {
                int stupci_int = 0,retci_int=0;
                for (int i = 0; i < stupci.Length; i++)
                {
                    stupci_int += stupci[i];
                }
                file.WriteLine(DateTime.Now + "stupci: " + stupci_int.ToString() + System.Environment.NewLine);
                for (int i = 0; i < retci.Length; i++)
                {
                    retci_int += retci[i];
                }
                file.WriteLine(DateTime.Now + "retci: " + retci_int.ToString() + System.Environment.NewLine);
                file.Write(DateTime.Now + "ByteArray: ");
                for (int i = 0; i < ms.Length; i++)
                {
                    file.Write(slika[i].ToString() + " ");
                }
                file.Write(System.Environment.NewLine);
                InfoBox.AppendText("Ispisano u Log!" + System.Environment.NewLine);
            }
            catch (Exception err)
            {
                file.WriteLine(DateTime.Now + "Error:" + err + System.Environment.NewLine);
            }
        }

        private void ExitButton_Click(object sender, FormClosingEventArgs e)
        {
                    if (serial1.IsOpen)
                        serial1.Close();
                    file.Close();
        }

        private void DiscBtn_Click(object sender, EventArgs e)
        {
            if (serial1.IsOpen)
            {
                serial1.Close();
                DiscBtn.Enabled = false;
            }
            comboBox1.Enabled=true;
        }

        private void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            sb.Append(indata);
            Console.Write(sb.Length+" ");
            Console.WriteLine(send_pointer);
            if (sb.Length==send_pointer)
            {
                ReplaceOldImage(sb.ToString());
            }
            file.Write(System.Environment.NewLine + DateTime.Now + "Buffer: ");
            file.Write(indata + " ");
        }

        private void ReplaceOldImage(String input){
            try
            {
               // InfoBox.AppendText("Received!" + System.Environment.NewLine);
                byte[] input_image = new byte[input.Length];
                input_image = Encoding.ASCII.GetBytes(input);
                byte[] image = ms.ToArray();
                int pocetak = image[10];
                int velicina = grayscale_foto_bit.Height * grayscale_foto_bit.Width * 4;
                int j = 0;
                for(int i = pocetak; i < velicina; i += 4)
                {
                    image[i] = input_image[j];
                    image[i + 1] = input_image[j];
                    image[i + 2] = input_image[j];
                    j++;
                }
                for (int i = pocetak; i < velicina; i += 4)
                {
                    if (image[i] > 0 && image[i] < 255)
                    {
                        image[i] = 255;
                        image[i + 1] = 255;
                        image[i + 2] = 255;
                    }
                }
                var ms_return = new MemoryStream(image);
                return_foto = Image.FromStream(ms_return);
                return_foto.Save("povratna_slika"+ DateTime.Now.ToString("dd-M-yyyy--HH-mm-ss") + ".bmp", ImageFormat.Bmp);
                pictureBox1.Image = return_foto;
                InfoBox.AppendText("Converted!" + System.Environment.NewLine);
                sb.Clear();
            }
            catch(Exception err)
            {
                file.WriteLine(DateTime.Now + "Error:" + err + System.Environment.NewLine);
            }
        }

        private void comboBoxClick(object sender, EventArgs e)
        {
            /* Source: https://www.codeproject.com/Articles/678025/Serial-Comms-in-Csharp-for-Beginners */
            try
            {
                string[] ArrayComPortsNames = null;
                int index = -1;
                string ComPortName = null;
                comboBox1.Items.Clear();

                ArrayComPortsNames = SerialPort.GetPortNames();
                do
                {
                    index += 1;
                    comboBox1.Items.Add(ArrayComPortsNames[index]);
                }

                while (!((ArrayComPortsNames[index] == ComPortName)
                              || (index == ArrayComPortsNames.GetUpperBound(0))));
                Array.Sort(ArrayComPortsNames);

                if (index == ArrayComPortsNames.GetUpperBound(0))
                {
                    ComPortName = ArrayComPortsNames[0];
                }
                comboBox1.Text = ArrayComPortsNames[0];
            }
            catch(Exception err)
            {
                comboBox1.Items.Clear();
                file.WriteLine(DateTime.Now + "Error: " + err + System.Environment.NewLine);
            }        
        }
    }
}
