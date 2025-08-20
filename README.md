# steg
A little C# toy utility for steganography

     Typical usage:
    > steg noise 256 noise256.bmp
    (Recipient must get a secure copy of the noise file.  For true one-time pad, it should never be reused.)
    > steg encrypt noise256.bmp hostimage.png outputimage.png "This is a secret message."
    (Upload outputimage.png anywhere; the recipient downloads it.)
    > steg decrypt noise256.bmp outputimage.png
