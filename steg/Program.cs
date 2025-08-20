using System.Runtime.Versioning;

namespace steg
{
    /*
     Typical usage:
    > steg noise 256 noise256.bmp
    (Recipient must get a secure copy of the noise file.  For true one-time pad, it should never be reused.)
    > steg encrypt noise256.bmp hostimage.png outputimage.png "This is a secret message."
    (Upload outputimage.png anywhere; the recipient downloads it.)
    > steg decrypt noise256.bmp outputimage.png
     */
    internal class Program
    {
        [SupportedOSPlatform("windows6.1")] // due to System.Drawing.Bitmap
        static void Main(string[] args)
        {
            // the first argument is the command verb and must be one of "noise", "encrypt" (or "enc"), or "decrypt"  (or "dec")
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: steg <command> [options]");
                Console.WriteLine("Commands:");
                Console.WriteLine("  noise     - Generate a one-bit noise bitmap image file");
                Console.WriteLine("  encrypt   - Encrypt a plaintext message into a host image");
                Console.WriteLine("  decrypt   - Decrypt a message from an image");
                return;
            }
            string command = args[0].ToLower();
            if (command == "noise")
            {
// the next two arguments should be an integer for the width and height of the square image and also a filename to write to
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: steg noise <width/height> <output filename>");
                    return;
                }
                if (!int.TryParse(args[1], out int width))
                {
                    Console.WriteLine("Width/height for the square image must be integers.");
                    return;
                }
                string outputFile = args[2];

                // generate a one-bit noise bitmap image file
                Stegodon.MakeNoiseImage(width, outputFile);
                Console.WriteLine($"Noise image ({width} x {width}) generated: " + outputFile);
                return;
            }

            else if (command == "encrypt" || command == "enc")
            {
                // the complete usage command line is:
                // steg encrypt <noise filename> <host image filename> <plaintext message filename>  <output image filename>
                if (args.Length < 5)
                {
                    Console.WriteLine("Usage: steg encrypt <noise filename> <host image filename> <output image filename> <\"plaintext message\">");
                    return;
                }
                string noiseFile = args[1];
                string hostImageFile = args[2];
                string plaintextFile = args[4];
                string outputImageFile = args[3];

                // call the EncryptMessage method with the provided parameters
                Stegodon.EncryptMessage(noiseFile, hostImageFile, plaintextFile, outputImageFile);
            }

            else if (command == "decrypt" || command == "dec")
            {
                // decrypt a message from a stego image
                // the complete usage command line is:
                // steg decrypt <noise filename> <image filename>
                if (args.Length < 3)
                {
                    Console.WriteLine("Usage: steg decrypt <noise filename> <image filename>");
                    return;
                }
                string noiseFile = args[1];
                string imageFile = args[2];
                // call the DecryptMessage method with the provided parameters
                Stegodon.DecryptMessage(noiseFile, imageFile);

            }
            else
            {
                Console.WriteLine("Unknown command: " + command);
                Console.WriteLine("Usage: steg <command> [options]");
                Console.WriteLine("Commands:");
                Console.WriteLine("  noise     - Generate a one-bit noise bitmap image file");
                Console.WriteLine("  encrypt   - Encrypt a plaintext message into a host image");
                Console.WriteLine("  decrypt   - Decrypt a message from an image");
                return;
            }



        }
    }
}
