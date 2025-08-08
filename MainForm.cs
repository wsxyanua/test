using System;
using System.Drawing;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace SteganoAES
{
    public class MainForm : Form
    {
        private Button btnSelectImage;
        private Button btnEncode;
        private Button btnDecode;
        private TextBox txtMessage;
        private TextBox txtPassword;
        private PictureBox pictureBox;
        private Label lblMessage;
        private Label lblPassword;

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(800, 600);
            this.Text = "Steganography + AES";

            btnSelectImage = new Button
            {
                Text = "Chọn ảnh",
                Location = new Point(20, 20),
                Size = new Size(100, 30)
            };
            btnSelectImage.Click += BtnSelectImage_Click;

            lblMessage = new Label
            {
                Text = "Nội dung:",
                Location = new Point(20, 70),
                Size = new Size(100, 20)
            };

            txtMessage = new TextBox
            {
                Location = new Point(20, 100),
                Size = new Size(300, 100),
                Multiline = true
            };

            lblPassword = new Label
            {
                Text = "Mật khẩu:",
                Location = new Point(20, 220),
                Size = new Size(100, 20)
            };

            txtPassword = new TextBox
            {
                Location = new Point(20, 250),
                Size = new Size(200, 20),
                UseSystemPasswordChar = true
            };

            btnEncode = new Button
            {
                Text = "Nhúng dữ liệu",
                Location = new Point(20, 290),
                Size = new Size(100, 30)
            };
            btnEncode.Click += BtnEncode_Click;

            btnDecode = new Button
            {
                Text = "Giải mã",
                Location = new Point(140, 290),
                Size = new Size(100, 30)
            };
            btnDecode.Click += BtnDecode_Click;

            pictureBox = new PictureBox
            {
                Location = new Point(350, 20),
                Size = new Size(400, 400),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            this.Controls.AddRange(new Control[] {
                btnSelectImage, lblMessage, txtMessage,
                lblPassword, txtPassword, btnEncode,
                btnDecode, pictureBox
            });
        }

        private void BtnSelectImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    pictureBox.Image = Image.FromFile(ofd.FileName);
                }
            }
        }

        private void BtnEncode_Click(object sender, EventArgs e)
        {
            if (pictureBox.Image == null || string.IsNullOrEmpty(txtMessage.Text) || string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin!");
                return;
            }

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(txtMessage.Text);
                byte[] salt = CryptoService.GenerateSalt();
                byte[] iv = CryptoService.GenerateIV();
                
                // Mã hóa dữ liệu
                byte[] encryptedData = CryptoService.Encrypt(messageBytes, txtPassword.Text, salt, iv);
                
                // Tạo metadata và blob
                byte[] blob = MetadataSerializer.Pack(salt, iv, encryptedData);
                
                // Nhúng blob vào ảnh
                Bitmap stegoImage = StegoService.EmbedData((Bitmap)pictureBox.Image.Clone(), blob);

                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = "stego_image.png"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    stegoImage.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show("Dữ liệu đã được nhúng thành công!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }
        }

        private void BtnDecode_Click(object sender, EventArgs e)
        {
            if (pictureBox.Image == null || string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Vui lòng chọn ảnh và nhập mật khẩu!");
                return;
            }

            try
            {
                // Trích xuất blob từ ảnh
                byte[] extractedBlob = StegoService.ExtractData((Bitmap)pictureBox.Image);
                
                // Parse metadata
                var metadata = MetadataSerializer.Unpack(extractedBlob, out byte[] encryptedData);
                
                // Giải mã
                byte[] decryptedData = CryptoService.Decrypt(encryptedData, txtPassword.Text, metadata.Salt, metadata.IV);
                string message = Encoding.UTF8.GetString(decryptedData);
                
                txtMessage.Text = message;
                MessageBox.Show("Giải mã thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: Mật khẩu không đúng hoặc ảnh không chứa dữ liệu hợp lệ.");
            }
        }
    }
}
