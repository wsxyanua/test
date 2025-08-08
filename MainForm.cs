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
        private Button? btnSelectImage;
        private Button? btnEncode;
        private Button? btnDecode;
        private TextBox? txtMessage;
        private TextBox? txtPassword;
        private PictureBox? pictureBox;
        private Label? lblMessage;
        private Label? lblPassword;

        private readonly CryptoService _cryptoService;
        private readonly StegoService _stegoService;
        private readonly MetadataSerializer _metadataSerializer;

        public MainForm()
        {
            InitializeComponents();
            _cryptoService = new CryptoService();
            _stegoService = new StegoService();
            _metadataSerializer = new MetadataSerializer();
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

        private void BtnSelectImage_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (pictureBox != null)
                    {
                        pictureBox.Image = Image.FromFile(ofd.FileName);
                    }
                }
            }
        }

        private void BtnEncode_Click(object? sender, EventArgs e)
        {
            if (pictureBox?.Image == null || string.IsNullOrEmpty(txtMessage?.Text) || string.IsNullOrEmpty(txtPassword?.Text))
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin!");
                return;
            }

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(txtMessage.Text);

                // Encrypt data
                byte[] encryptedData = _cryptoService.Encrypt(messageBytes, txtPassword.Text, out byte[] salt, out byte[] iv, out byte[] hmac);

                // Pack metadata and ciphertext into a single blob
                byte[] blob = _metadataSerializer.Pack(salt, iv, hmac, encryptedData);

                // Embed the blob into the image
                Bitmap stegoImage = _stegoService.Embed((Bitmap)pictureBox.Image.Clone(), blob);

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

        private void BtnDecode_Click(object? sender, EventArgs e)
        {
            if (pictureBox?.Image == null || string.IsNullOrEmpty(txtPassword?.Text))
            {
                MessageBox.Show("Vui lòng chọn ảnh và nhập mật khẩu!");
                return;
            }

            try
            {
                Bitmap image = (Bitmap)pictureBox.Image;

                // Step 1: Read a fixed-size header to determine the total size of the payload.
                // The header contains: Magic(4) + Ver(1) + Flags(2) + SaltLen(2) + Salt(16) + IVLen(2) + IV(16) + HmacLen(4) + Hmac(32) + CiphertextLen(4)
                const int headerSizeToReadLengths = 83;
                byte[] headerBytes = _stegoService.Extract(image, headerSizeToReadLengths);

                int ciphertextLength;
                using (var ms = new MemoryStream(headerBytes))
                using (var reader = new BinaryReader(ms))
                {
                    // Skip fields before the ciphertext length
                    reader.BaseStream.Seek(4 + 1 + 2, SeekOrigin.Current); // Magic, Version, Flags
                    ushort saltLength = reader.ReadUInt16();
                    reader.BaseStream.Seek(saltLength, SeekOrigin.Current); // Salt
                    ushort ivLength = reader.ReadUInt16();
                    reader.BaseStream.Seek(ivLength, SeekOrigin.Current); // IV
                    int hmacLength = reader.ReadInt32();
                    reader.BaseStream.Seek(hmacLength, SeekOrigin.Current); // HMAC

                    // Read the ciphertext length
                    ciphertextLength = reader.ReadInt32();
                }

                // Step 2: Calculate the full blob size and extract it.
                int totalBlobSize = headerSizeToReadLengths + ciphertextLength;
                byte[] fullBlob = _stegoService.Extract(image, totalBlobSize);

                // Step 3: Unpack the full blob.
                var metadata = _metadataSerializer.Unpack(fullBlob);

                // Step 4: Decrypt.
                if (metadata.Ciphertext == null || metadata.Salt == null || metadata.Iv == null || metadata.Hmac == null)
                {
                    throw new InvalidDataException("Extracted metadata is incomplete.");
                }
                byte[] decryptedData = _cryptoService.Decrypt(metadata.Ciphertext, txtPassword.Text, metadata.Salt, metadata.Iv, metadata.Hmac);
                string message = Encoding.UTF8.GetString(decryptedData);

                if (txtMessage != null)
                {
                    txtMessage.Text = message;
                }
                MessageBox.Show("Giải mã thành công!");
            }
            catch (CryptographicException)
            {
                MessageBox.Show("Giải mã thất bại. Mật khẩu không đúng hoặc dữ liệu đã bị thay đổi.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
