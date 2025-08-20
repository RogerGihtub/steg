using System.Drawing;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace steg
{
    public class Stegodon  // thanks to  https://github.com/2alf/Stegodon
    {
        // making the noise image
        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static void MakeNoiseImage(int size, string filename)
        {
            try
            {
                using var bitmap = new System.Drawing.Bitmap(size, size);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int bit = RandomNumberGenerator.GetInt32(2);
                        bitmap.SetPixel(x, y, bit == 1 ? System.Drawing.Color.Black : System.Drawing.Color.White);
                    }
                }
                bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);

                //Console.WriteLine($"Bitmap of size {size}x{size} saved to {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }



        // PREPOCESSING STARTS HERE
        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static Bitmap NormalizeImage(Bitmap originalImage)
        {
            Bitmap normalizedImage = new(originalImage.Width, originalImage.Height);

            using (Graphics graphics = Graphics.FromImage(normalizedImage))
            {
                graphics.DrawImage(originalImage, Point.Empty);
            }

            for (int y = 0; y < normalizedImage.Height; y++)
            {
                for (int x = 0; x < normalizedImage.Width; x++)
                {
                    Color pixel = normalizedImage.GetPixel(x, y);
                    Color newPixel = Color.FromArgb(
                        pixel.R % 2 != 0 ? pixel.R - 1 : pixel.R, // if odd make even
                        pixel.G % 2 != 0 ? pixel.G - 1 : pixel.G,
                        pixel.B % 2 != 0 ? pixel.B - 1 : pixel.B);

                    normalizedImage.SetPixel(x, y, newPixel);
                }
            }
            return normalizedImage;
        }


        // PREPOCESSING ENDS HERE

        // ENCRYPTION STARTS HERE

        // Stegodon.EncryptMessage(noiseFile, hostImageFile, plaintextFile, outputImageFile);

        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static void EncryptMessage(string noiseFile, string hostImageFile, string plaintextFile, string outputImageFile)
        {

            // load the one-bit bitmap image file 
            string noisePath = noiseFile; // e.g. @"c:\temp\argal\noise001.bmp"

            // ensure the file exists
            if (!System.IO.File.Exists(noisePath))
            {
                Console.WriteLine($"Error: Noise file does not exist: {noisePath}");
                return;
            }


            string otpNoise = Stegodon.GetNoiseString(noisePath);



            // randomly generate a uint16 offset value using cryptographic random number generator
            // using System.Security.Cryptography;
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[2]; // 2 bytes for uint16
            rng.GetBytes(randomBytes);
            // convert the random bytes to a uint16 value
            ushort offsetValue = BitConverter.ToUInt16(randomBytes, 0);
            // print the offset value
            Console.WriteLine("Offset Value:  " + offsetValue);

            // string path to plaintext message file c:\temp\argal\plaintext.txt
            string plaintextPath = plaintextFile; // e.g. @"c:\temp\argal\plaintext.txt"

            /*
            // ensure the file exists
            if (!System.IO.File.Exists(plaintextPath))
            {
                Console.WriteLine($"Error: Plaintext file does not exist: {plaintextPath}");
                return;
            }
            */

            // read the plaintext message file
           // string plaintextMessage = System.IO.File.ReadAllText(plaintextPath);
            string plaintextMessage = plaintextPath;

            // print the plaintext message length   
           Console.WriteLine("Plaintext Message Length: " + plaintextMessage.Length);
            // convert the plaintext message to binary  
            string binaryMessage = Stegodon.Txt2Bin(plaintextMessage);
            // remove any " " characters from the binary message
            binaryMessage = binaryMessage.Replace(" ", "");
            // print the binary message length
            Console.WriteLine("Binary Message Length: " + binaryMessage.Length);

            // encryypt the binary message using the offset value and the noise string
            string cryptText = Stegodon.GetOffsetXOR(offsetValue, otpNoise, binaryMessage);

            // put the cryptText length into a uint16
            UInt16 cryptTextLength = (UInt16)cryptText.Length;

            // print the cryptText length
            //Console.WriteLine("CryptText Length: " + cryptTextLength);

            // print the first 64 characters of the cryptText
            //Console.WriteLine("CryptText:      " + cryptText.Substring(0, 64) + "...");


            // construct the vector that will be inserted into the image
            // the vector is the offset value in binary, the length of the message in binary, and the cryptText
            // first 2 bytes are the offset value in binary, next 2 bytes are the length of the message in binary, and the rest is the cryptText
            // then padded out to the end with more noise bits from anywhere.

            string offsetBinary = Stegodon.UInt16ToBin(offsetValue);
            string messageLengthBinary = Stegodon.UInt16ToBin(cryptTextLength);
            // combine the offsetBinary, messageLengthBinary, and cryptText into a single string
            string vector = offsetBinary + messageLengthBinary + cryptText;
            // print the vector length
            //Console.WriteLine("Vector Length: " + vector.Length);

            // load up the host image file
            string hostImagePath = hostImageFile; // e.g. @"c:\temp\argal\host001.bmp"

            // ensure the file exists
            if (!System.IO.File.Exists(hostImagePath))
            {
                Console.WriteLine($"Error: Host image file does not exist: {hostImagePath}");
                return;
            }

            // load the host image
            Bitmap hostImage = new(hostImagePath);

            // print the host image size
            Console.WriteLine("Host Image Size: " + hostImage.Width + "x" + hostImage.Height);
            // print the host image pixel format
            Console.WriteLine("Host Image Pixel Format: " + hostImage.PixelFormat);
            // print the host image bits per pixel
            Console.WriteLine("Host Image Bits Per Pixel: " + Image.GetPixelFormatSize(hostImage.PixelFormat));
            // print the host image total pixels
            Console.WriteLine("Host Image Total Pixels: " + (hostImage.Width * hostImage.Height));

            // ensure the host image is large enough to hold the vector
            if (hostImage.Width * hostImage.Height < vector.Length)
            {
                Console.WriteLine("Error: Host image is not large enough to hold the vector.");
                return;
            }

            // pad out the vector to the end with noise bits "0" and "1" from random cryptographic generator
            RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
            int vectorLength = vector.Length;
            int totalPixels = hostImage.Width * hostImage.Height;
            if (vectorLength < totalPixels)
            {
                int paddingLength = totalPixels - vectorLength;
                for (int i = 0; i < paddingLength; i++)
                {
                    // generate a random bit "0" or "1"
                    byte[] randomByte = new byte[1];
                    rng2.GetBytes(randomByte);
                    char randomBit = (randomByte[0] % 2 == 0) ? '0' : '1';
                    vector += randomBit;
                }
            }

            // print the first 64 characters of the vector
            //Console.WriteLine("Vector:         " + vector.Substring(0, 64) + "...");

            // stegodon encrypt the vector into the host image
            Bitmap stegoImage = Stegodon.Encrypt(hostImage, vector);
            // save the stego image to a file
            string stegoImagePath = outputImageFile; // e.g. @"c:\temp\argal\stego001.bmp"
            // save image in PNG format

            System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png; // save as PNG

            stegoImage.Save(stegoImagePath,format);

            Console.WriteLine($"Output image saved to: {stegoImagePath}");


        }

        public static string Txt2Bin(string text)
        {
            StringBuilder binaryMessage = new();

            foreach (char character in text)
            {
                string binaryChar = Convert.ToString(character, 2);

                // pad with 0 until binaryChar == 1 byte
                binaryChar = binaryChar.PadLeft(8, '0');

                binaryMessage.Append(binaryChar).Append(' ');
            }

            if (binaryMessage.Length > 0)
            {
                binaryMessage.Length--; // remove the last char space ( empty space ) 
            }

            return binaryMessage.ToString();
        }


        // UInt8ToBin converts a byte to a binary string representation
        public static string UInt8ToBin(byte value)
        {
            StringBuilder binaryString = new(8);
            for (int i = 7; i >= 0; i--)
            {
                binaryString.Append((value & (1 << i)) != 0 ? '1' : '0');
            }
            return binaryString.ToString();
        }

        //UInt16ToBin converts a ushort to a binary string representation
        public static string UInt16ToBin(ushort value)
        {
            StringBuilder binaryString = new(16);
            for (int i = 15; i >= 0; i--)
            {
                binaryString.Append((value & (1 << i)) != 0 ? '1' : '0');
            }
            return binaryString.ToString();
        }

        // UInt64ToBin converts a ulong to a binary string representation
        public static string UInt64ToBin(ulong value)
        {
            StringBuilder binaryString = new(64);
            for (int i = 63; i >= 0; i--)
            {
                binaryString.Append((value & (1UL << i)) != 0 ? '1' : '0');
            }
            return binaryString.ToString();
        }


        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        private static Bitmap InjectBinMsg(Bitmap normalizedImage, string binaryMessage)
        {
            Bitmap encryptedImage = new(normalizedImage);

            binaryMessage = binaryMessage.Replace(" ", ""); // remove emptys

            int bitIndex = 0; // pointer

            for (int y = 0; y < encryptedImage.Height; y++)
            {
                for (int x = 0; x < encryptedImage.Width; x++)
                {
                    if (bitIndex < binaryMessage.Length) // check
                    {
                        Color pixel = encryptedImage.GetPixel(x, y);

                        Color newPixel = Color.FromArgb( // update the least significant bit of each color channel with a bit from the string
                            (pixel.R & 0xFE) | ((bitIndex < binaryMessage.Length) ? ((binaryMessage[bitIndex] - '0') & 1) : 0),
                            (pixel.G & 0xFE) | (((bitIndex + 1) < binaryMessage.Length) ? ((binaryMessage[bitIndex + 1] - '0') & 1) : 0),
                            (pixel.B & 0xFE) | (((bitIndex + 2) < binaryMessage.Length) ? ((binaryMessage[bitIndex + 2] - '0') & 1) : 0));

                        encryptedImage.SetPixel(x, y, newPixel);

                        bitIndex += 3; // shift to the next 3 bits -/- pixel
                    }
                    else
                    {
                        return encryptedImage; // IF all bits are used then copy//recycle the remaining pixels
                    }
                }
            }

            return encryptedImage;
        }

        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static Bitmap Encrypt(Bitmap originalImage, string inputString)
        {
            Bitmap normalizedImage = NormalizeImage(originalImage);

            // it's already in this format
            //string binaryMessage = Txt2Bin(inputString); // goat --> 01100111 01101111 01100001 01110100 00001010

            Bitmap encryptedImage = InjectBinMsg(normalizedImage, inputString);

            return encryptedImage;
        }

        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static Bitmap EncryptOriginal(Bitmap originalImage, string inputString)
        {
            Bitmap normalizedImage = NormalizeImage(originalImage);

            string binaryMessage = Txt2Bin(inputString); // goat --> 01100111 01101111 01100001 01110100 00001010

            Bitmap encryptedImage = InjectBinMsg(normalizedImage, binaryMessage);

            return encryptedImage;
        }

        // ENCRYPTION ENDS HERE



        // DECRYPTION STARTS HERE

        // Stegodon.DecryptMessage(noiseFile, imageFile);
        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static void DecryptMessage(string noiseFile, string imageFile)
        {
            // load the noise file
            string noisePath = noiseFile; // e.g. @"c:\temp\argal\noise001.bmp"
            // ensure the file exists
            if (!System.IO.File.Exists(noisePath))
            {
                Console.WriteLine($"Error: Noise file does not exist: {noisePath}");
                return;
            }
            string otpNoise = Stegodon.GetNoiseString(noisePath);
            // load the image file
            string imagePath = imageFile; // e.g. @"c:\temp\argal\stego001.bmp"
            // ensure the file exists
            if (!System.IO.File.Exists(imagePath))
            {
                Console.WriteLine($"Error: Image file does not exist: {imagePath}");
                return;
            }

            // now we will decrypt the message from the stego image
            // load the stego image
            Bitmap stegoImage2 = new(imagePath);
            // print the stego image size
            Console.WriteLine("Image Size: " + stegoImage2.Width + "x" + stegoImage2.Height);
            // print the stego image pixel format
            Console.WriteLine("Image Pixel Format: " + stegoImage2.PixelFormat);
            // print the stego image bits per pixel
            Console.WriteLine("Image Bits Per Pixel: " + Image.GetPixelFormatSize(stegoImage2.PixelFormat));
            // print the stego image total pixels
            Console.WriteLine("Image Total Pixels: " + (stegoImage2.Width * stegoImage2.Height));

            // decrypt the vector from the stego image
            string extractedVector = Stegodon.Extract(stegoImage2);
            // print the extracted vector (first 64 characters)
           // Console.WriteLine("Extracted Vector: " + extractedVector.Substring(0, 64) + "...");
            // extract the offset value from the extracted vector
            string extractedOffsetBinary = extractedVector[..16]; // first 16 bits are the offset
            string extractedMessageLengthBinary = extractedVector.Substring(16, 16); // next 16 bits are the message length
            string extractedCryptText = extractedVector[32..]; // the rest is the cryptText
            // print the extracted offset value
            //Console.WriteLine("Extracted Offset Binary: " + extractedOffsetBinary);
            // convert the extracted offset binary to a ushort value
            ushort extractedOffsetValue = Convert.ToUInt16(extractedOffsetBinary, 2);
            // print the extracted offset value
            Console.WriteLine("Extracted Offset Value: " + extractedOffsetValue);
            // print the extracted message length binary
           // Console.WriteLine("Extracted Message Length Binary: " + extractedMessageLengthBinary);
            // convert the extracted message length binary to a ushort value
            ushort extractedMessageLengthValue = Convert.ToUInt16(extractedMessageLengthBinary, 2);
            // print the extracted message length value
            Console.WriteLine("Extracted Message Length Value: " + extractedMessageLengthValue);
            // print the extracted cryptText (first 64 characters)
           // Console.WriteLine("Extracted CryptText: " + extractedCryptText.Substring(0, 64) + "...");

            // now we will decrypt the extracted cryptText using the extracted offset value and the otpNoise

            // truncate extractedCryptText to the length of extractedMessageLengthValue
            if (extractedCryptText.Length > extractedMessageLengthValue)
            {
                extractedCryptText = extractedCryptText[..extractedMessageLengthValue];
            }


            // encryypt the binary message using the offset value and the noise string
            string newPlainText = Stegodon.GetOffsetXOR(extractedOffsetValue, otpNoise, extractedCryptText);

            // remove any " " characters from the newPlainText
            newPlainText = newPlainText.Replace(" ", "");

            // turn newPlainText back into text and print first 64 characters
            string decryptedPlainText = Stegodon.Bin2Txt(newPlainText);

            // print the newPlainText length
            Console.WriteLine("Plaintext Length: " + decryptedPlainText.Length);

            // write --- MESSAGE START --- to console
            Console.WriteLine("--- MESSAGE START ---");


            Console.WriteLine(decryptedPlainText);
            // write --- MESSAGE END --- to console
            Console.WriteLine("--- MESSAGE END ---");




        }


        public static string Bin2Txt(string binaryMessage)
        {
            StringBuilder messageBuilder = new();

            for (int i = 0; i < binaryMessage.Length; i += 8) // 8bit
            {
                if (i + 8 <= binaryMessage.Length) // at least 8 characters remaining in the binary to convert to ASCII
                {
                    string byteStr = binaryMessage.Substring(i, 8);
                    int charCode = Convert.ToInt32(byteStr, 2);
                    char character = (char)charCode;
                    messageBuilder.Append(character);
                    // Console.Write(character); //for debugging purposes
                }
                else
                {
                    break;
                }
            }

            return messageBuilder.ToString();
        }

        // Txt2UInt8 converts a string of binary to a byte value
        public static byte Txt2UInt8(string binaryString)
        {
            if (binaryString.Length != 8)
            {
                throw new ArgumentException("Binary string must be exactly 8 characters long.");
            }
            byte value = 0;
            for (int i = 0; i < 8; i++)
            {
                if (binaryString[i] == '1')
                {
                    value |= (byte)(1 << (7 - i)); // set the bit at position i
                }
            }
            return value;
        }

        // Txt2UInt16 converts a string of binary to a ushort value
        public static ushort Txt2UInt16(string binaryString)
        {
            if (binaryString.Length != 16)
            {
                throw new ArgumentException("Binary string must be exactly 16 characters long.");
            }
            ushort value = 0;
            for (int i = 0; i < 16; i++)
            {
                if (binaryString[i] == '1')
                {
                    value |= (ushort)(1 << (15 - i)); // set the bit at position i
                }
            }
            return value;
        }




        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static string Extract(Bitmap image)
        {
            StringBuilder messageBuilder = new();

            for (int y = 0; y < image.Height; y++) // pixel iteration and exctract least significant bit from each channel
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    messageBuilder.Append(pixel.R & 1);
                    messageBuilder.Append(pixel.G & 1);
                    messageBuilder.Append(pixel.B & 1);
                }
            }

            string binaryMessage = messageBuilder.ToString(); // 0110011101101111011000010111010000001010??????... - ? as img data
            //string message = Bin2Txt(binaryMessage); // 01100111 01101111 01100001 01110100 00001010 --> goat

            return binaryMessage; //message;
        }

        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static string DecryptOriginal(Bitmap image)
        {
            StringBuilder messageBuilder = new();

            for (int y = 0; y < image.Height; y++) // pixel iteration and exctract least significant bit from each channel
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    messageBuilder.Append(pixel.R & 1);
                    messageBuilder.Append(pixel.G & 1);
                    messageBuilder.Append(pixel.B & 1);
                }
            }

            string binaryMessage = messageBuilder.ToString(); // 0110011101101111011000010111010000001010??????... - ? as img data
            string message = Bin2Txt(binaryMessage); // 01100111 01101111 01100001 01110100 00001010 --> goat

            return message;
        }

        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static string GetNoiseString(string filename)
            {
            Bitmap image = new(filename);
            StringBuilder noiseBuilder = new();
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    noiseBuilder.Append(pixel.R & 1); // extract the least significant bit of the red channel
                }
            }
            return noiseBuilder.ToString();
        }

        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        public static string GetNoiseString(string filename, UInt32 offset, int length)
        {
            Bitmap image = new(filename);
            StringBuilder noiseBuilder = new();

            // move offset number of pixels into the image, across and down width and height
            int startX = (int)(offset % image.Width);
            int startY = (int)(offset / image.Width);
            int currentX = startX;
            int currentY = startY;
            int count = 0;
            for (int y = startY; y < image.Height && count < length; y++)
            {
                for (int x = startX; x < image.Width && count < length; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    noiseBuilder.Append(pixel.R & 1); // extract the least significant bit of the red channel
                    count++;
                    currentX++;
                    if (currentX >= image.Width) // wrap around to the next row
                    {
                        currentX = 0;
                        currentY++;
                    }
                }
                startX = 0; // reset startX after the first row
            }


            return noiseBuilder.ToString();
        }



        public static string GetOffsetXOR(UInt64 offset, string noise, string message)
        {
            // This method applies an XOR operation between the noise and the message at a given offset.
            StringBuilder xorResult = new();
            UInt64 noiseLength = (UInt64)noise.Length;
            for (UInt64 i = 0; i < (UInt64)message.Length; i++)
// both noise and message consist of literals like "0" and "1" so produce XOR quasi-manually:
            {
                char noiseBit = noise[(int)((UInt64)(i + offset) % (UInt64)noiseLength)]; // wrap around if offset exceeds noise length
                char messageBit = message[(int)i];
                xorResult.Append((noiseBit == messageBit) ? '0' : '1'); // XOR operation
            }
            return xorResult.ToString();

        }

        // DECRYPTION ENDS HERE
    }
    /*
MIT License

Copyright (c) 2024 2alf

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.     
     */
}

