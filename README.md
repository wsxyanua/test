# Steganography + AES Encryption in C#

This project is a C# application (WinForms) that allows users to embed and extract hidden data within images using the Least Significant Bit (LSB) steganography technique, combined with strong AES-256 encryption.

## Features

- **Embed Data**: Encrypts a text message or a small file with AES-256 and hides it within a carrier image (PNG, BMP).
- **Extract Data**: Recovers the hidden data from a steganographic image and decrypts it.
- **Strong Encryption**: Uses AES-256-CBC for confidentiality, PBKDF2 for key derivation, and HMAC-SHA256 for integrity and authenticity.
- **Robust Metadata**: A well-defined metadata structure is embedded alongside the ciphertext to ensure reliable extraction.

## Technical Specifications

### 1. Cryptography

- **Algorithm**: AES-256 in CBC mode.
- **Key Derivation**: `Rfc2898DeriveBytes` (PBKDF2 with HMAC-SHA256) is used to generate a 256-bit key from a user-provided passphrase.
  - **Salt**: A random 16-byte salt is generated for each encryption and stored in the metadata.
  - **Iterations**: 100,000 iterations to deter brute-force attacks.
- **Initialization Vector (IV)**: A random 16-byte IV is generated for each encryption and stored in the metadata.
- **Padding**: PKCS7 padding is used by default.
- **Data Integrity**: An HMAC-SHA256 tag is computed over the ciphertext and stored in the metadata to protect against tampering.

### 2. Metadata Format

A binary blob is constructed and embedded into the LSBs of the image. The structure is as follows:

| Field              | Size (bytes)      | Description                                      |
|--------------------|-------------------|--------------------------------------------------|
| **Magic Header**   | 4                 | `0x53 0x54 0x45 0x47` ("STEG") to identify the format. |
| **Version**        | 1                 | Format version (e.g., `0x01`).                   |
| **Flags**          | 2                 | Reserved for future use (e.g., compression type).|
| **Salt Length**    | 2 (ushort)        | Length of the salt data.                         |
| **Salt**           | `n` (variable)    | The 16-byte salt for PBKDF2.                     |
| **IV Length**      | 2 (ushort)        | Length of the IV data.                           |
| **IV**             | `m` (variable)    | The 16-byte IV for AES-CBC.                      |
| **Ciphertext Length**| 4 (int)           | Length of the encrypted data.                    |
| **HMAC**           | 32                | HMAC-SHA256 of the ciphertext.                   |
| **Ciphertext**     | `L` (variable)    | The encrypted data.                              |

### 3. LSB Steganography

- **Embedding Strategy**: Data is written into the least significant bit of each color channel (Red, Green, Blue). This provides 3 bits of storage per pixel.
- **Supported Formats**: Lossless image formats like **PNG** and **BMP** are required. Using lossy formats like JPEG will corrupt the embedded data.
- **Capacity Check**: The application verifies if the carrier image has enough capacity to store the data blob before proceeding.

## How to Build and Run

1. **Prerequisites**:
   - .NET 6.0 SDK or later.
   - Visual Studio 2022 or a compatible IDE.

2. **Build**:
   - Clone the repository.
   - Open the `SteganoAES.sln` file in Visual Studio.
   - Build the solution (Ctrl+Shift+B).

3. **Run**:
   - Run the project from Visual Studio (F5).

## Usage

### To Embed Data (Encode)

1. Launch the application.
2. Click **"Load Image"** and select a PNG or BMP image.
3. In the "Text to Embed" field, type the secret message.
4. Enter a strong password in the "Password" field.
5. Click **"Embed"**.
6. The application will prompt you to save the new image (e.g., `output.png`).
7. A success message will be displayed upon completion.

### To Extract Data (Decode)

1. Launch the application.
2. Click **"Load Image"** and select an image that contains hidden data.
3. Enter the correct password in the "Password" field.
4. Click **"Extract"**.
5. The hidden message will be displayed in the "Text to Embed" field.
6. If the password is wrong or the image is tampered with, an error message will be shown.

## Project Structure

- `MainForm.cs`: The main GUI window and event handling logic.
- `CryptoService.cs`: Handles all cryptographic operations (encryption, decryption, key derivation).
- `StegoService.cs`: Implements the LSB embedding and extraction logic.
- `MetadataSerializer.cs`: Manages the packing and unpacking of the metadata and ciphertext blob.
